using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BattleTurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    [SerializeField] private float turnTransitionDelay = 1f;

    [Header("References")]
    [SerializeField] private HexGrid hexGrid;

    // Списки юнитов по тегам Unity (Player1Unit = Игрок 1, Player2Unit = Игрок 2)
    private List<BattleHexUnit> playerUnits = new List<BattleHexUnit>(); // Команда с тегом "Player1Unit"
    private List<BattleHexUnit> enemyUnits = new List<BattleHexUnit>(); // Команда с тегом "Player2Unit"

    private bool isPlayer1Turn = true; // true = ход команды Player1Unit (Игрок 1), false = ход команды Player2Unit (Игрок 2)
    private BattleHexUnit currentActiveUnit;
    private int currentUnitIndex = 0;

    public static BattleTurnManager Instance { get; private set; }

    List<BattleHexUnit> currentTeam;
    
    private BattleConfig battleConfig;

        // События для UI
    public event System.Action OnUnitsInitialized;
    public event System.Action<BattleHexUnit> OnActiveUnitChanged;
    public event System.Action<bool> OnTurnChanged; // true - Player1 (Player1Unit), false - Player2 (Player2Unit)

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(InitializeTurnSystem());
    }

    private IEnumerator InitializeTurnSystem()
    {
        yield return new WaitForSeconds(0.5f); // Ждем инициализации карты

        // Получаем конфиг из меню выбора
        // BattleManager уже проверил конфиг, поэтому просто получаем его без повторного предупреждения
        battleConfig = BattleSettingsManager.CurrentConfig;
        if (battleConfig == null)
        {
            // Если конфиг все еще null (что не должно происходить, если BattleManager работает правильно),
            // создаем дефолтный, но без предупреждения, так как BattleManager уже вывел его
            battleConfig = new BattleConfig();
        }

        FindAllUnits();
        
        // Уведомляем UI что юниты готовы
        OnUnitsInitialized?.Invoke();
        currentTeam = playerUnits; // Устанавливаем команду игрока 1
        
        // Определяем, с какой команды начинать (та, у которой ControlType == Human)
        if (IsTeamHumanControlled(true))
        {
            StartPlayer1Turn(); // Начинаем ход игрока 1 (Player1Unit)
        }
        else
        {
            StartPlayer2Turn(); // Начинаем ход игрока 2 (Player2Unit)
        }
    }

    private void FindAllUnits()
    {
        BattleHexUnit[] allUnits = FindObjectsByType<BattleHexUnit>(FindObjectsSortMode.None);

        playerUnits.Clear();
        enemyUnits.Clear();

        foreach (BattleHexUnit unit in allUnits)
        {
            if (unit.IsAlive)
            {
                if (unit.CompareTag("Player1Unit"))
                    playerUnits.Add(unit);
                else if (unit.CompareTag("Player2Unit"))
                    enemyUnits.Add(unit);
            }
        }

        Debug.Log($"Найдено юнитов: {playerUnits.Count} игрок 1 (Player1Unit), {enemyUnits.Count} игрок 2 (Player2Unit)");
    }

    public void StartPlayer1Turn()
    {
        isPlayer1Turn = true;
        currentUnitIndex = 0;
        currentTeam = playerUnits; // Явно устанавливаем команду Player1Unit (Игрок 1)

        // Восстанавливаем stamina всем юнитам игрока 1
        foreach (BattleHexUnit unit in playerUnits)
        {
            if (unit.IsAlive) unit.ResetStamina();
        }
        
        // Обновляем подсветку всех юнитов команды 1
        UpdateAllUnitsHighlight(playerUnits, false);

        StartNextUnitTurn();
        Debug.Log("=== ХОД ИГРОКА 1 (Player1Unit) ===");
        OnTurnChanged?.Invoke(true);
    }

    public void StartPlayer2Turn()
    {
        isPlayer1Turn = false;
        currentUnitIndex = 0;
        currentTeam = enemyUnits; // Явно устанавливаем команду Player2Unit (Игрок 2)

        // Восстанавливаем stamina всем юнитам игрока 2
        foreach (BattleHexUnit unit in enemyUnits)
        {
            if (unit.IsAlive) unit.ResetStamina();
        }
        
        // Обновляем подсветку всех юнитов команды 2
        UpdateAllUnitsHighlight(enemyUnits, false);

        StartNextUnitTurn();
        Debug.Log("=== ХОД ИГРОКА 2 (Player2Unit) ===");
        OnTurnChanged?.Invoke(false);
    }

    private void StartNextUnitTurn()
    {
        // Убедимся что currentTeam установлена
        if (currentTeam == null)
        {
            currentTeam = isPlayer1Turn ? playerUnits : enemyUnits;
        }

        // Убираем мертвых юнитов из списка
        currentTeam.RemoveAll(unit => !unit.IsAlive);

        // Если все юниты команды сходили - завершаем ход команды
        if (currentUnitIndex >= currentTeam.Count)
        {
            EndTeamTurn();
            return;
        }

        currentActiveUnit = currentTeam[currentUnitIndex];

        // Если юнит мертв - пропускаем
        if (!currentActiveUnit.IsAlive)
        {
            currentUnitIndex++;
            StartNextUnitTurn();
            return;
        }

        // Активируем юнита (SelectUnit уже обновляет подсветку)
        SelectUnit(currentUnitIndex);

        // Если текущая команда управляется ИИ - запускаем AI
        if (!IsCurrentTeamHumanControlled())
        {
            StartCoroutine(AITurn());
        }

        Debug.Log($"Активный юнит: {currentActiveUnit.name} (Управление: {(IsCurrentTeamHumanControlled() ? "Человек" : "ИИ")})");
    }


    private IEnumerator AITurn()
    {
        yield return new WaitForSeconds(1f);
        EndCurrentUnitTurn();
    }

    private void EndTeamTurn()
    {
        StartCoroutine(TransitionToNextTeam());
    }

    private IEnumerator TransitionToNextTeam()
    {
        yield return new WaitForSeconds(turnTransitionDelay);

        if (isPlayer1Turn)
            StartPlayer2Turn();
        else
            StartPlayer1Turn();
    }
    
    
    // Методы для UI

    public void PlayerUnitFinishedTurn()
    {
        // Проверяем, что текущая команда управляется человеком
        if (IsCurrentTeamHumanControlled() && currentActiveUnit != null)
        {
            // Останавливаем анимацию текущего активного юнита перед завершением хода
            currentActiveUnit.EndBattleTurn();
            
            EndTeamTurn();
        }
    }
    
    /// <summary>
    /// Проверяет, управляется ли текущая команда человеком
    /// </summary>
    private bool IsCurrentTeamHumanControlled()
    {
        return IsTeamHumanControlled(isPlayer1Turn);
    }
    
    /// <summary>
    /// Проверяет, управляется ли указанная команда человеком
    /// </summary>
    /// <param name="isPlayer1Team">true для Player1Unit (Игрок 1), false для Player2Unit (Игрок 2)</param>
    private bool IsTeamHumanControlled(bool isPlayer1Team)
    {
        if (battleConfig == null)
        {
            // По умолчанию: Player1Unit = человек, Player2Unit = ИИ (старая логика)
            return isPlayer1Team;
        }
        
        if (isPlayer1Team)
        {
            return battleConfig.player1ControlType == ControlType.Human;
        }
        else
        {
            return battleConfig.player2ControlType == ControlType.Human;
        }
    }
    
    /// <summary>
    /// Возвращает список юнитов команды Player1 (тег "Player1Unit")
    /// </summary>
    public List<BattleHexUnit> GetPlayer1Units()
    {
        return playerUnits;
    }
    
    /// <summary>
    /// Возвращает список юнитов команды Player2 (тег "Player2Unit")
    /// </summary>
    public List<BattleHexUnit> GetPlayer2Units()
    {
        return enemyUnits;
    }
    
    /// <summary>
    /// Возвращает список юнитов, управляемых человеком
    /// </summary>
    public List<BattleHexUnit> GetHumanControlledUnits()
    {
        List<BattleHexUnit> humanUnits = new List<BattleHexUnit>();
        
        if (IsTeamHumanControlled(true))
        {
            humanUnits.AddRange(playerUnits);
        }
        
        if (IsTeamHumanControlled(false))
        {
            humanUnits.AddRange(enemyUnits);
        }
        
        return humanUnits;
    }
    
    /// <summary>
    /// Возвращает список юнитов текущей активной команды, если она управляется человеком
    /// </summary>
    public List<BattleHexUnit> GetCurrentTeamHumanControlledUnits()
    {
        if (currentTeam == null)
        {
            return new List<BattleHexUnit>();
        }
        
        // Проверяем, управляется ли текущая команда человеком
        if (IsCurrentTeamHumanControlled())
        {
            return new List<BattleHexUnit>(currentTeam);
        }
        
        return new List<BattleHexUnit>();
    }
    
    public BattleHexUnit GetCurrentActiveUnit()
    {
        return currentActiveUnit;
    }

    public void SelectUnit(int index)
    {
        if (currentTeam == null) return;

        if (index >= 0 && index < currentTeam.Count)
        {
            BattleHexUnit unit = currentTeam[index];

            if (unit.IsAlive)
            {
                // Деактивируем предыдущего активного юнита
                if (currentActiveUnit != null && currentActiveUnit != unit)
                {
                    currentActiveUnit.EndBattleTurn();
                }
                
                // Деактивируем предыдущего активного юнита (подсветка)
                if (currentActiveUnit != null && currentActiveUnit != unit && currentActiveUnit.cardRenderer != null)
                {
                    bool prevIsTeam1 = currentActiveUnit.CompareTag("Player1Unit");
                    currentActiveUnit.cardRenderer.UpdateHighlight(prevIsTeam1, false);
                }

                // Устанавливаем нового активного юнита
                currentActiveUnit = unit;
                currentActiveUnit.StartBattleTurn(); // Внутри уже вызывается UpdateHighlight()

                // Обновляем индекс
                currentUnitIndex = index;
                
                // Подсветка уже обновлена в StartBattleTurn(), но для уверенности обновляем еще раз
                if (currentActiveUnit.cardRenderer != null)
                {
                    bool isTeam1 = currentActiveUnit.CompareTag("Player1Unit");
                    currentActiveUnit.cardRenderer.UpdateHighlight(isTeam1, true);
                }

                Debug.Log($"Выбран юнит: {unit.name}");

                // Уведомляем UI
                OnActiveUnitChanged?.Invoke(unit);
            }
        }
    }
    
    /// <summary>
    /// Обновляет подсветку всех юнитов в команде
    /// </summary>
    /// <param name="team">Список юнитов команды</param>
    /// <param name="active">Флаг активности (обычно false, активным становится только currentActiveUnit)</param>
    private void UpdateAllUnitsHighlight(List<BattleHexUnit> team, bool active)
    {
        foreach (BattleHexUnit unit in team)
        {
            if (unit != null && unit.IsAlive && unit.cardRenderer != null)
            {
                // Определяем команду по тегу: Player1Unit = Team1, Player2Unit = Team2
                bool isTeam1 = unit.CompareTag("Player1Unit");
                // Активным является только текущий активный юнит
                bool isActive = active && unit == currentActiveUnit;
                unit.cardRenderer.UpdateHighlight(isTeam1, isActive);
            }
        }
    }
    
    /// <summary>
    /// Выбирает юнит напрямую по ссылке на объект
    /// </summary>
    public void SelectUnitByUnit(BattleHexUnit unit)
    {
        if (unit == null || !unit.IsAlive) return;
        
        // Находим индекс юнита в соответствующем списке
        int index = -1;
        if (playerUnits.Contains(unit))
        {
            index = playerUnits.IndexOf(unit);
        }
        else if (enemyUnits.Contains(unit))
        {
            index = enemyUnits.IndexOf(unit);
        }
        
        if (index >= 0)
        {
            SelectUnit(index);
        }
        else
        {
            Debug.LogWarning($"Unit {unit.name} not found in player or enemy units list");
        }
    }
    
    
    // Обновляем метод EndCurrentUnitTurn
    public void EndCurrentUnitTurn()
    {
        if (currentActiveUnit != null)
        {
            currentActiveUnit.EndBattleTurn();
        }
        
        currentUnitIndex++;
        StartNextUnitTurn();
    }

}