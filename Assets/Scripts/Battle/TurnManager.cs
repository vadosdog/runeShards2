using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BattleTurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    [SerializeField] private float turnTransitionDelay = 1f;

    [Header("References")]
    [SerializeField] private HexGrid hexGrid;

    // Списки юнитов по тегам Unity (PlayerUnit = Игрок 1, EnemyUnit = Игрок 2)
    private List<BattleHexUnit> playerUnits = new List<BattleHexUnit>(); // Команда с тегом "PlayerUnit"
    private List<BattleHexUnit> enemyUnits = new List<BattleHexUnit>(); // Команда с тегом "EnemyUnit"

    private bool isPlayer1Turn = true; // true = ход команды PlayerUnit (Игрок 1), false = ход команды EnemyUnit (Игрок 2)
    private BattleHexUnit currentActiveUnit;
    private int currentUnitIndex = 0;

    public static BattleTurnManager Instance { get; private set; }

    List<BattleHexUnit> currentTeam;
    
    private BattleConfig battleConfig;

    // События для UI
    public event System.Action OnUnitsInitialized;
    public event System.Action<BattleHexUnit> OnActiveUnitChanged;
    public event System.Action<bool> OnTurnChanged; // true - Player1 (PlayerUnit), false - Player2 (EnemyUnit)

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
        battleConfig = BattleSettingsManager.CurrentConfig;
        if (battleConfig == null)
        {
            battleConfig = new BattleConfig();
            Debug.LogWarning("BattleConfig не найден, использую настройки по умолчанию");
        }

        FindAllUnits();
        
        // Уведомляем UI что юниты готовы
        OnUnitsInitialized?.Invoke();
        currentTeam = playerUnits; // Устанавливаем команду игрока 1
        
        // Определяем, с какой команды начинать (та, у которой ControlType == Human)
        if (IsTeamHumanControlled(true))
        {
            StartPlayer1Turn(); // Начинаем ход игрока 1 (PlayerUnit)
        }
        else
        {
            StartPlayer2Turn(); // Начинаем ход игрока 2 (EnemyUnit)
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
                if (unit.CompareTag("PlayerUnit"))
                    playerUnits.Add(unit);
                else if (unit.CompareTag("EnemyUnit"))
                    enemyUnits.Add(unit);
            }
        }

        Debug.Log($"Найдено юнитов: {playerUnits.Count} игрок 1 (PlayerUnit), {enemyUnits.Count} игрок 2 (EnemyUnit)");
    }

    public void StartPlayer1Turn()
    {
        isPlayer1Turn = true;
        currentUnitIndex = 0;
        currentTeam = playerUnits; // Явно устанавливаем команду PlayerUnit (Игрок 1)

        // Восстанавливаем stamina всем юнитам игрока 1
        foreach (BattleHexUnit unit in playerUnits)
        {
            if (unit.IsAlive) unit.ResetStamina();
        }

        StartNextUnitTurn();
        Debug.Log("=== ХОД ИГРОКА 1 (PlayerUnit) ===");
        OnTurnChanged?.Invoke(true);
    }

    public void StartPlayer2Turn()
    {
        isPlayer1Turn = false;
        currentUnitIndex = 0;
        currentTeam = enemyUnits; // Явно устанавливаем команду EnemyUnit (Игрок 2)

        // Восстанавливаем stamina всем юнитам игрока 2
        foreach (BattleHexUnit unit in enemyUnits)
        {
            if (unit.IsAlive) unit.ResetStamina();
        }

        StartNextUnitTurn();
        Debug.Log("=== ХОД ИГРОКА 2 (EnemyUnit) ===");
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

        // Активируем юнита
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
    /// <param name="isPlayer1Team">true для PlayerUnit (Игрок 1), false для EnemyUnit (Игрок 2)</param>
    private bool IsTeamHumanControlled(bool isPlayer1Team)
    {
        if (battleConfig == null)
        {
            // По умолчанию: PlayerUnit = человек, EnemyUnit = ИИ (старая логика)
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
    /// Возвращает список юнитов команды Player1 (тег "PlayerUnit")
    /// </summary>
    public List<BattleHexUnit> GetPlayer1Units()
    {
        return playerUnits;
    }
    
    /// <summary>
    /// Возвращает список юнитов команды Player2 (тег "EnemyUnit")
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

                // Устанавливаем нового активного юнита
                currentActiveUnit = unit;
                currentActiveUnit.StartBattleTurn();

                // Обновляем индекс
                currentUnitIndex = index;

                Debug.Log($"Выбран юнит: {unit.name}");

                // Уведомляем UI
                OnActiveUnitChanged?.Invoke(unit);
            }
        }
    }
    
    /// <summary>
    /// Выбирает юнит напрямую по ссылке на объект
    /// </summary>
    public void SelectUnitByUnit(BattleHexUnit unit)
    {
        if (unit == null || !unit.IsAlive) return;
        
        // Определяем, в какой команде находится юнит
        List<BattleHexUnit> team = null;
        bool isPlayer1Team = playerUnits.Contains(unit);
        
        if (isPlayer1Team)
        {
            team = playerUnits;
            // Если текущая команда не playerUnits, нужно переключиться
            if (!isPlayer1Turn)
            {
                Debug.LogWarning("Попытка выбрать юнита из PlayerUnit (Игрок 1) во время хода EnemyUnit (Игрок 2)");
                return;
            }
        }
        else if (enemyUnits.Contains(unit))
        {
            team = enemyUnits;
            // Если текущая команда не enemyUnits, нужно переключиться
            if (isPlayer1Turn)
            {
                Debug.LogWarning("Попытка выбрать юнита из EnemyUnit (Игрок 2) во время хода PlayerUnit (Игрок 1)");
                return;
            }
        }
        else
        {
            Debug.LogWarning("Юнит не найден ни в одной из команд");
            return;
        }
        
        int index = team.IndexOf(unit);
        if (index >= 0)
        {
            SelectUnit(index);
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