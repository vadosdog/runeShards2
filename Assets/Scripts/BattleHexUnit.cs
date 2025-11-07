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
    public int maxStamina = 5;
    public int currentStamina = 5;
    
    [Header("Battle Visuals")]
    public GameObject selectionHighlight;
    public GameObject healthBar;
    
    // Свойства для управления состоянием в битве
    public bool IsActive { get; set; }
    public bool IsAlive => currentHealth > 0;

    private HexGrid grid
    {
        get
        {
            if (_grid == null)
                _grid = FindFirstObjectByType<HexGrid>();
            return _grid;
        }
    }
    private HexGrid _grid;


    // Переопределяем метод для инициализации в битве
    protected void Start()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    // Боевые методы
    public void StartBattleTurn()
    {
        IsActive = true;
        ShowSelectionHighlight(true);
        ResetStamina();
    }

    public void EndBattleTurn()
    {
        IsActive = false;
        ShowSelectionHighlight(false);
    }

    public void ResetStamina()
    {
        currentStamina = maxStamina;
    }

    public bool CanMoveTo(HexCell targetCell)
    {
        if (!IsActive || !IsAlive) return false;
        
        // Проверяем стоимость перемещения
        int moveCost = CalculateMoveCost(targetCell);
        return moveCost <= currentStamina && IsValidDestination(targetCell);
    }

    public void BattleMoveTo(HexCell targetCell)
    {
        if (!CanMoveTo(targetCell)) return;

        // Используем путь вместо прямой телепортации
        //List<HexCell> path = grid.FindPath(Location, targetCell, this);
        grid.FindPath(Location, targetCell, this);
        // if (path != null && path.Count > 0)
        // {
        //     int moveCost = path.Count; // Упрощенная стоимость
        //     currentStamina -= moveCost;
            
        //     // Используем оригинальный метод Travel
        // }
        Travel(grid.GetPath());
        
        Debug.Log($"{name} переместился. Stamina: {currentStamina}");
    }

    public void Attack(BattleHexUnit target)
    {
        if (!IsActive || !IsAlive || currentStamina < 1) return;

        int damage = Mathf.Max(1, attack - target.defense);
        target.TakeDamage(damage);
        currentStamina -= 1;

        Debug.Log($"{name} атаковал {target.name}. Урон: {damage}, Stamina: {currentStamina}");
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"{name} получил {damage} урона. Здоровье: {currentHealth}");

        if (currentHealth <= 0)
        {
            BattleDie();
        }
    }

    private void BattleDie()
    {
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

    private int CalculateMoveCost(HexCell targetCell)
    {
        // Используем существующую логику расчета стоимости перемещения
        // или создаем упрощенную версию для битвы
        return GetMoveCost(Location, targetCell, (HexDirection)Random.Range(0, 6));
    }

    private void ShowSelectionHighlight(bool show)
    {
        if (selectionHighlight != null)
            selectionHighlight.SetActive(show);
    }

    // Визуализация в редакторе для отладки
    void OnDrawGizmosSelected()
    {
        if (IsActive && IsAlive)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
            
            // Показываем текущий stamina радиусом
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, currentStamina * 2f);
        }
    }
}