using UnityEngine;
using System.Collections.Generic;

// Расширяем оригинальный HexUnit для битвы
public class BattleHexUnit : HexUnit
{
    [Header("Battle Stats")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    public int attack = 10;
    public int defense = 5;
    public int maxStamina = 10;
    public int currentStamina = 10;
    
    [Header("Battle Visuals")]
    public GameObject healthBar;
    
    // Свойства для управления состоянием в битве
    public bool IsActive { get; set; }
    public bool IsAlive => currentHealth > 0;

    private BattleHexGrid grid
    {
        get
        {
            if (_grid == null)
                _grid = FindFirstObjectByType<BattleHexGrid>();
            return _grid;
        }
    }
    private BattleHexGrid _grid;
    
    // События для отслеживания изменений
    public event System.Action<BattleHexUnit> OnHealthChanged;
    public event System.Action<BattleHexUnit> OnStaminaChanged;
    public event System.Action<BattleHexUnit> OnUnitDied;


    // Переопределяем метод для инициализации в битве
    protected void Start()
    {
        currentHealth = maxHealth;
        ResetStamina();
    }

    // Боевые методы
    public void StartBattleTurn()
    {
        IsActive = true;
    }

    public void EndBattleTurn()
    {
        IsActive = false;
    }

    public void ResetStamina()
    {
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(this);
    }

    public void BattleMoveTo()
    {
        if (!grid.HasPath) return;

        int moveCost = grid.MoveCost;
        Travel(grid.GetPath());
        currentStamina -= moveCost;

        // Уведомляем об изменении стамины
        OnStaminaChanged?.Invoke(this);
        
        Debug.Log($"{name} переместился. Stamina: {currentStamina}");
    }

    // TODO переделать
    public void Attack(BattleHexUnit target)
    {
        if (!IsActive || !IsAlive || currentStamina < 1) return;

        int damage = Mathf.Max(1, attack - target.defense);
        target.TakeDamage(damage);
        currentStamina -= 1;

        // Уведомляем об изменении стамины
        OnStaminaChanged?.Invoke(this);

        Debug.Log($"{name} атаковал {target.name}. Урон: {damage}, Stamina: {currentStamina}");
    }

    // TODO переделать
    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);

        // Уведомляем об изменении здоровья
        OnHealthChanged?.Invoke(this);
        Debug.Log($"{name} получил {damage} урона. Здоровье: {currentHealth}");

        if (currentHealth <= 0)
        {
            BattleDie();
        }
    }

    // TODO переделать
    private void BattleDie()
    {
        // Уведомляем о смерти юнита
        OnUnitDied?.Invoke(this);
        Debug.Log($"{name} погиб в битве!");

        Die();

        // Визуальные эффекты смерти
        GetComponent<Renderer>().material.color = Color.gray;

        // Отключаем коллайдеры
        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        IsActive = false;
    }

    public override int GetMoveCost(
		HexCell fromCell, HexCell toCell, HexDirection direction)
	{
		if (!IsValidDestination(toCell))
		{
			return -1;
		}
		HexEdgeType edgeType = HexMetrics.GetEdgeType(
			fromCell.Values.Elevation, toCell.Values.Elevation);
		if (edgeType == HexEdgeType.Cliff)
		{
			return -1;
		}
		int moveCost;
		if (fromCell.Flags.HasRoad(direction))
		{
			moveCost = 1;
		}
		else if (fromCell.Flags.HasAny(HexFlags.Walled) !=
			toCell.Flags.HasAny(HexFlags.Walled))
		{
			return -1;
		}
		else
        {
			moveCost = edgeType == HexEdgeType.Flat ? 1 : 2;
			HexValues v = toCell.Values;
			moveCost += v.UrbanLevel + v.FarmLevel + v.PlantLevel;
		}
		return moveCost;
	}
}