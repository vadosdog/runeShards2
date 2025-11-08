using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BattleTurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    [SerializeField] private float turnTransitionDelay = 1f;

    [Header("References")]
    [SerializeField] private HexGrid hexGrid;

    private HexCell previouslyHighlightedCell;

    private List<BattleHexUnit> playerUnits = new List<BattleHexUnit>();
    private List<BattleHexUnit> enemyUnits = new List<BattleHexUnit>();

    private bool isPlayerTurn = true;
    private BattleHexUnit currentActiveUnit;
    private int currentUnitIndex = 0;

    public static BattleTurnManager Instance { get; private set; }
    public bool IsPlayerTurn => isPlayerTurn;

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

        StartPlayerTurn();
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

        StartNextUnitTurn();
        Debug.Log("=== ХОД ИГРОКА ===");
    }

    public void StartEnemyTurn()
    {
        isPlayerTurn = false;
        currentUnitIndex = 0;

        StartNextUnitTurn();
        Debug.Log("=== ХОД ПРОТИВНИКА ===");
    }

    private void StartNextUnitTurn()
    {
        List<BattleHexUnit> currentTeam = isPlayerTurn ? playerUnits : enemyUnits;

        // Убираем мертвых юнитов из списка
        currentTeam.RemoveAll(unit => !unit.IsAlive);

        if (currentUnitIndex >= currentTeam.Count)
        {
            EndTeamTurn();
            return;
        }

        currentActiveUnit = currentTeam[currentUnitIndex];

        if (!currentActiveUnit.IsAlive)
        {
            currentUnitIndex++;
            StartNextUnitTurn();
            return;
        } else
        {
            
            SelectPlayerUnit(currentUnitIndex);
        }

        currentActiveUnit.StartBattleTurn();

        // Уведомляем UI о смене активного юнита
        OnActiveUnitChanged?.Invoke(currentActiveUnit);
        OnTurnChanged?.Invoke(isPlayerTurn);

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
            EndCurrentUnitTurn();
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

    public void SelectPlayerUnit(int index)
    {
        Debug.Log($"SELECTED {index}");
        if (!isPlayerTurn) return;

        if (index >= 0 && index < playerUnits.Count)
        {
            BattleHexUnit unit = playerUnits[index];

            if (unit.IsAlive)
            {
                // Убираем подсветку с предыдущего юнита
                ClearUnitHighlight();

                // Устанавливаем нового активного юнита
                currentActiveUnit = unit;
                currentActiveUnit.StartBattleTurn();

                // Подсвечиваем нового активного юнита
                HighlightUnit(unit);

                // Обновляем индекс для порядка ходов
                currentUnitIndex = index;

                Debug.Log($"Выбран юнит: {unit.name}");

                // Уведомляем UI о смене активного юнита
                OnActiveUnitChanged?.Invoke(unit);
            }
        }
    }
    
    private void HighlightUnit(BattleHexUnit unit)
    {
        if (unit?.Location == null) return;
        
        // Подсвечиваем ячейку юнита
        hexGrid.HighlightUnitCell(unit.Location.Index);
        previouslyHighlightedCell = unit.Location;
    }
    
    private void ClearUnitHighlight()
    {
        // Убираем подсветку с предыдущей ячейки
        if (previouslyHighlightedCell != null)
        {
            hexGrid.DisableHighlight(previouslyHighlightedCell.Index);
        }
    }
    
    // Обновляем метод EndCurrentUnitTurn
    public void EndCurrentUnitTurn()
    {
        if (currentActiveUnit != null)
        {
            currentActiveUnit.EndBattleTurn();
            ClearUnitHighlight();
        }
        
        currentUnitIndex++;
        StartNextUnitTurn();
    }

}