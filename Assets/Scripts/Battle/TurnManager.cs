using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BattleTurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    [SerializeField] private float turnTransitionDelay = 1f;

    [Header("References")]
    [SerializeField] private HexGrid hexGrid;

    private List<BattleHexUnit> playerUnits = new List<BattleHexUnit>();
    private List<BattleHexUnit> enemyUnits = new List<BattleHexUnit>();

    private bool isPlayerTurn = true;
    private BattleHexUnit currentActiveUnit;
    private int currentUnitIndex = 0;

    public static BattleTurnManager Instance { get; private set; }
    public bool IsPlayerTurn => isPlayerTurn;

    List<BattleHexUnit> currentTeam;

    // События для UI
    public event System.Action OnUnitsInitialized;
    public event System.Action<BattleHexUnit> OnActiveUnitChanged;
    public event System.Action<bool> OnTurnChanged; // true - player, false - enemy

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

        FindAllUnits();
        
        // Уведомляем UI что юниты готовы
        OnUnitsInitialized?.Invoke();
        currentTeam = playerUnits; // Устанавливаем команду игрока
        StartPlayerTurn(); // Начинаем ход игрока
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

        Debug.Log($"Найдено юнитов: {playerUnits.Count} игрок, {enemyUnits.Count} враг");
    }

    public void StartPlayerTurn()
    {
        isPlayerTurn = true;
        currentUnitIndex = 0;
        currentTeam = playerUnits; // Явно устанавливаем команду

        // Восстанавливаем stamina всем юнитам игрока
        foreach (BattleHexUnit unit in playerUnits)
        {
            if (unit.IsAlive) unit.ResetStamina();
        }

        StartNextUnitTurn();
        Debug.Log("=== ХОД ИГРОКА ===");
        OnTurnChanged?.Invoke(true);
    }

    public void StartEnemyTurn()
    {
        isPlayerTurn = false;
        currentUnitIndex = 0;
        currentTeam = enemyUnits; // Явно устанавливаем команду

        // Восстанавливаем stamina всем юнитам врага
        foreach (BattleHexUnit unit in enemyUnits)
        {
            if (unit.IsAlive) unit.ResetStamina();
        }

        StartNextUnitTurn();
        Debug.Log("=== ХОД ПРОТИВНИКА ===");
        OnTurnChanged?.Invoke(false);
    }


    private void StartNextUnitTurn()
    {
        // Убедимся что currentTeam установлена
        if (currentTeam == null)
        {
            currentTeam = isPlayerTurn ? playerUnits : enemyUnits;
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

        // Если ход врага - запускаем AI
        if (!isPlayerTurn)
        {
            StartCoroutine(EnemyAITurn());
        }

        Debug.Log($"Активный юнит: {currentActiveUnit.name}");
    }


    private IEnumerator EnemyAITurn()
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

        if (isPlayerTurn)
            StartEnemyTurn();
        else
            StartPlayerTurn();
    }

    // Вспомогательные методы для AI
    private BattleHexUnit FindNearestPlayerUnit()
    {
        BattleHexUnit nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (BattleHexUnit playerUnit in playerUnits)
        {
            if (!playerUnit.IsAlive) continue;

            float distance = Vector3.Distance(
                currentActiveUnit.transform.position,
                playerUnit.transform.position
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = playerUnit;
            }
        }

        return nearest;
    }
    
    
    // Методы для UI

    public void PlayerUnitFinishedTurn()
    {
        if (isPlayerTurn && currentActiveUnit != null)
        {
            EndTeamTurn();
        }
    }
    public List<BattleHexUnit> GetPlayerUnits()
    {
        return playerUnits;
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