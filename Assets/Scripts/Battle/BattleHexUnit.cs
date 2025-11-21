using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Расширяем оригинальный HexUnit для битвы
public class BattleHexUnit : HexUnit
{
    [Header("Battle Stats")]
    public int currentHealth = 100;
    public int currentStamina = 10;
    
    [Header("Battle Visuals")]
    public GameObject healthBar;
    
    [Header("Unit Card Display")]
    public UnitCardRenderer cardRenderer; // Публичное для доступа из BattleManager
    
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

    private BattleHexGrid _grid;
    
    private BattleHexGrid grid
    {
        get
        {
            if (_grid == null)
                _grid = FindFirstObjectByType<BattleHexGrid>();
            return _grid;
        }
    }
    
    // События для отслеживания изменений
    public event System.Action<BattleHexUnit> OnHealthChanged;
    public event System.Action<BattleHexUnit> OnStaminaChanged;
    public event System.Action<BattleHexUnit> OnUnitDied;


    public void InitializeFromUnitData(UnitData data, bool isPlayerUnit = true)
    {
        if (data == null)
        {
            Debug.LogError("UnitData is null");
            return;
        }

        unitData = data;
        
        currentHealth = data.maxHealth;
        currentStamina = data.maxStamina;
        
        // Инициализируем карточку юнита
        SetupUnitCard(isPlayerUnit);
        
        // Обновляем подсветку после инициализации
        UpdateHighlight();
    }
    
    /// <summary>
    /// Настраивает отображение карточки юнита
    /// </summary>
    /// <param name="isPlayerUnit">Устаревший параметр. Используется только для определения зеркалирования.
    /// Команда определяется по тегу GameObject (Player1Unit = Team1, Player2Unit = Team2)</param>
    private void SetupUnitCard(bool isPlayerUnit = true)
    {
        if (unitData == null)
        {
            return;
        }
        
        // Получаем или создаем компонент UnitCardRenderer
        cardRenderer = GetComponent<UnitCardRenderer>();
        if (cardRenderer == null)
        {
            cardRenderer = gameObject.AddComponent<UnitCardRenderer>();
        }
        
        // Определяем команду по тегу: Player1Unit = Team1, Player2Unit = Team2
        bool isTeam1 = CompareTag("Player1Unit");
        
        // Team2 (Player2Unit) зеркалируется по оси X для визуального различия
        bool shouldFlipX = !isTeam1;
        
        // Инициализируем карточку из UnitData
        if (cardRenderer != null)
        {
            cardRenderer.InitializeFromUnitData(unitData, flipX: shouldFlipX, isTeam1: isTeam1);
        }
        
        // Отключаем или удаляем старые 3D визуальные компоненты (Cube и т.д.)
        // Ищем дочерние объекты с MeshRenderer (старые 3D модели)
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            
            // Пропускаем объекты с SpriteRenderer (наша карточка, если она на дочернем объекте)
            if (child.GetComponent<SpriteRenderer>() != null)
                continue;
            
            // Отключаем MeshRenderer (старые 3D модели)
            MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
            
            // Настраиваем коллайдер на основном объекте, если он есть на дочернем
            BoxCollider childCollider = child.GetComponent<BoxCollider>();
            if (childCollider != null)
            {
                // Переносим коллайдер на основной объект, если его там еще нет
                BoxCollider mainCollider = GetComponent<BoxCollider>();
                if (mainCollider == null)
                {
                    mainCollider = gameObject.AddComponent<BoxCollider>();
                    // Настраиваем размер коллайдера для карточки (примерно соответствует размеру карточки)
                    mainCollider.size = new Vector3(2f, 3f, 0.1f);
                    mainCollider.center = new Vector3(0f, 3f, 0f); // Центр на высоте карточки (6/2 = 3)
                }
                
                // Отключаем коллайдер на дочернем объекте
                childCollider.enabled = false;
            }
        }
        
        // Отключаем 3D рендереры на основном объекте, если они есть
        Renderer[] renderers = GetComponents<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // Не отключаем SpriteRenderer (это наш UnitCardRenderer)
            if (!(renderer is SpriteRenderer))
            {
                renderer.enabled = false;
            }
        }
    }
    
    void OnEnable()
    {
        // Повторяем логику базового OnEnable, так как он недоступен для переопределения
        // Проверяем, что Grid установлен (Grid устанавливается в AddUnit(), который вызывается после Instantiate())
        // Если Grid еще не установлен, OnEnable будет вызван снова после AddUnit()
        if (Grid != null)
        {
            try
            {
                // Пытаемся получить Location (может быть null, если locationCellIndex еще не установлен)
                HexCell location = Location;
                if (location != null)
                {
                    transform.localPosition = location.Position;
                    
                    // Обновляем позицию карточки с учетом высоты гекса
                    if (cardRenderer != null)
                    {
                        cardRenderer.UpdatePositionWithHexElevation();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки, если Grid или location еще не инициализированы
                // OnEnable будет вызван снова после правильной инициализации
            }
        }
        
        // Обновляем подсветку при включении юнита
        UpdateHighlight();
    }

    // Боевые методы
    public void StartBattleTurn()
    {
        IsActive = true;
        
        // Включаем аниматор и устанавливаем Idle анимацию
        if (cardRenderer != null)
        {
            cardRenderer.EnableAnimator();
            cardRenderer.SetIdleAnimation();
        }
        
        // Обновляем подсветку при активации юнита
        UpdateHighlight();
    }

    public void EndBattleTurn()
    {
        IsActive = false;
        
        // Отключаем аниматор (статичное состояние) когда юнит неактивен
        if (cardRenderer != null)
        {
            cardRenderer.SetStaticState();
        }
        
        // Обновляем подсветку при деактивации юнита
        UpdateHighlight();
    }
    
    /// <summary>
    /// Обновляет подсветку на основе команды и активности
    /// </summary>
    private void UpdateHighlight()
    {
        if (cardRenderer != null)
        {
            bool isTeam1 = CompareTag("Player1Unit");
            cardRenderer.UpdateHighlight(isTeam1, IsActive);
        }
    }
    
    /// <summary>
    /// Выполняет атаку (вызывается перед использованием навыка)
    /// </summary>
    public void PerformAttack()
    {
        if (cardRenderer != null)
        {
            cardRenderer.PlayAttackAnimation();
        }
        
        // Возвращаемся к Idle после анимации атаки
        StartCoroutine(ResetToIdleAfterAttack());
    }
    
    private System.Collections.IEnumerator ResetToIdleAfterAttack()
    {
        // Ждем завершения анимации атаки (примерно 0.5 секунды)
        yield return new WaitForSeconds(0.5f);
        
        if (cardRenderer != null)
        {
            cardRenderer.SetIdleAnimation();
        }
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
        
        // Устанавливаем анимацию движения перед перемещением
        if (cardRenderer != null)
        {
            cardRenderer.SetMoveAnimation(true);
        }
        
        Travel(grid.GetPath());
        ConsumeStamina(moveCost);

        Debug.Log($"{name} переместился. Stamina: {currentStamina}");
        
        // После завершения перемещения возвращаемся к Idle
        // Это будет вызвано после завершения Travel через корутину
        StartCoroutine(ResetToIdleAfterMove());
    }
    
    private System.Collections.IEnumerator ResetToIdleAfterMove()
    {
        // Ждем завершения перемещения (примерно 1-2 секунды в зависимости от скорости)
        yield return new WaitForSeconds(2f);
        
        if (cardRenderer != null)
        {
            cardRenderer.SetIdleAnimation();
        }
    }

    /// <summary>
    /// Применяет урон к юниту
    /// </summary>
    /// <param name="damage">Количество урона</param>
    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);

        // Проигрываем анимацию получения урона (временно включаем аниматор если юнит неактивен)
        Debug.Log($"{name} получает {damage} урона. IsActive: {IsActive}, cardRenderer: {(cardRenderer != null ? "exists" : "null")}");
        if (cardRenderer != null)
        {
            cardRenderer.PlayHurtAnimation(IsActive);
        }
        else
        {
            Debug.LogWarning($"{name}: cardRenderer is null, cannot play hurt animation");
        }

        // Уведомляем об изменении здоровья
        OnHealthChanged?.Invoke(this);
        Debug.Log($"{name} получил {damage} урона. Здоровье: {currentHealth}");

        if (currentHealth <= 0)
        {
            HandleUnitDeath();
        }
        else
        {
            // Возвращаемся к правильному состоянию после анимации урона
            StartCoroutine(ResetAfterHurt());
        }
    }
    
    private System.Collections.IEnumerator ResetAfterHurt()
    {
        // Ждем завершения анимации урона (примерно 0.5 секунды)
        yield return new WaitForSeconds(0.5f);
        
        if (cardRenderer != null)
        {
            // Возвращаем в правильное состояние: Idle для активных, статичное для неактивных
            if (IsActive)
            {
                cardRenderer.SetIdleAnimation();
            }
            else
            {
                cardRenderer.SetStaticState();
            }
        }
    }

    /// <summary>
    /// Обрабатывает смерть юнита
    /// </summary>
    private void HandleUnitDeath()
    {
        // Уведомляем о смерти юнита
        OnUnitDied?.Invoke(this);
        Debug.Log($"{name} погиб в битве!");

        // Визуальные эффекты смерти - затемняем карточку
        if (cardRenderer != null)
        {
            cardRenderer.SetColor(Color.gray);
        }
        else
        {
            // Fallback для старой системы
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.gray;
            }
        }

        // Отключаем коллайдеры
        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        IsActive = false;
        
        // Удаляем юнита
        Die();
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

    /// <summary>
    /// Validate the position of the unit and update card position.
    /// </summary>
    public new void ValidateLocation()
    {
        base.ValidateLocation();
        
        // Обновляем позицию карточки с учетом высоты нового гекса
        if (cardRenderer != null)
        {
            cardRenderer.UpdatePositionWithHexElevation();
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