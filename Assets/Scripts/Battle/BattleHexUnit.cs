using UnityEngine;
using System.Collections.Generic;

// Расширяем оригинальный HexUnit для битвы
public class BattleHexUnit : HexUnit
{
    [Header("Battle Stats")]
    public int currentHealth = 100;
    public int currentStamina = 10;
    
    [Header("Battle Visuals")]
    public GameObject healthBar;
    
    [Header("Unit Data")]
    public UnitData unitData;
    
    // Свойства для управления состоянием в битве
    public bool IsActive { get; set; }
    public bool IsAlive => currentHealth > 0;


    public int PhyAtk => unitData?.PhyAtk ?? 1;
    public int MagAtk => unitData?.MagAtk ?? 1;
    public int PhyDef => unitData?.PhyDef ?? 1;
    public int MagDef => unitData?.MagDef ?? 1;
    public int maxHealth => unitData?.maxHealth ?? 100;
    public int maxStamina => unitData?.maxStamina ?? 10;
    public ElementType elementType => unitData?.elementType ?? ElementType.None;
    public ShapeType shapeType => unitData?.shapeType ?? ShapeType.Beast;
    public EmotionType emotionType => unitData?.emotionType ?? EmotionType.Rage;
    public List<AbstractBattleSkill> Skills => unitData?.Skills ?? new List<AbstractBattleSkill>();

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


    public void InitializeFromUnitData(UnitData data)
    {
        if (data == null)
        {
            Debug.LogError("UnitData is null");
            return;
        }

        unitData = data;
        
        currentHealth = data.maxHealth;
        currentStamina = data.maxStamina;
        
        // Настраиваем визуал (если нужно)
        // GetComponent<MeshRenderer>().material = data.unitMaterial;
    }
    
    protected void Start()
    {
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

    public void ConsumeStamina(int amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
        OnStaminaChanged?.Invoke(this);
    }

    public void BattleMoveTo()
    {
        if (!grid.HasPath) return;

        int moveCost = grid.MoveCost;
        Travel(grid.GetPath());
        ConsumeStamina(moveCost);

        Debug.Log($"{name} переместился. Stamina: {currentStamina}");
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

    /// <summary>
    /// Check whether a cell is a valid destination for the unit.
    /// Не проверяет флаг Explored, если туман войны отключен.
    /// </summary>
    /// <param name="cell">Cell to check.</param>
    /// <returns>Whether the unit could occupy the cell.</returns>
    public override bool IsValidDestination(HexCell cell)
    {
        // Проверяем, включен ли туман войны
        bool fogOfWarEnabled = false;
        if (grid != null)
        {
            fogOfWarEnabled = grid.FogOfWarEnabled;
        }
        
        // Если туман войны отключен, не проверяем флаг Explored
        if (fogOfWarEnabled)
        {
            return cell.Flags.HasAll(HexFlags.Explored | HexFlags.Explorable) &&
                !cell.Values.IsUnderwater && !cell.Unit;
        }
        else
        {
            return cell.Flags.HasAll(HexFlags.Explorable) &&
                !cell.Values.IsUnderwater && !cell.Unit;
        }
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
            // Базовая стоимость зависит только от направления изменения высоты
            int elevationDiff = toCell.Values.Elevation - fromCell.Values.Elevation;
            if (elevationDiff > 0)
            {
                // Подъем: стоимость 2
                moveCost = 2;
            }
            else
            {
                // Плоская поверхность или спуск: стоимость 1
                moveCost = 1;
            }
        }
        return moveCost;
    }
    
    public bool HasStatus(StatusType statusType)
    {
        return false; // Добавить проверку на статусы
    }
}