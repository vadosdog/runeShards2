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
        }

        currentActiveUnit.StartBattleTurn();

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

        // BattleHexUnit nearestPlayer = FindNearestPlayerUnit();

        // if (nearestPlayer != null)
        // {
        //     // Находим лучшую позицию для атаки
        //     HexCell bestMoveCell = FindBestMovePosition(nearestPlayer.Location);

        //     if (bestMoveCell != null && bestMoveCell != currentActiveUnit.Location)
        //     {
        //         currentActiveUnit.BattleMoveTo(bestMoveCell);
        //         yield return new WaitForSeconds(0.5f);
        //     }

        //     // Проверяем возможность атаки
        //     if (CanAttackTarget(nearestPlayer))
        //     {
        //         currentActiveUnit.Attack(nearestPlayer);
        //         yield return new WaitForSeconds(0.5f);
        //     }
        // }

        // EndCurrentUnitTurn();
    }

    public void EndCurrentUnitTurn()
    {
        if (currentActiveUnit != null)
        {
            currentActiveUnit.EndBattleTurn();
        }

        currentUnitIndex++;
        StartNextUnitTurn();
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

    // private HexCell FindBestMovePosition(HexCell targetCell)
    // {
    //     // Ищем клетки в радиусе stamina для перемещения к цели
    //     int searchRadius = currentActiveUnit.currentStamina;
    //     HexCell bestCell;
    //     float bestScore = float.MinValue;

    //     // Простой перебор соседних клеток вместо GetCellsInRange
    //     for (int distance = 1; distance <= searchRadius; distance++)
    //     {
    //         foreach (HexCell cell in GetCellsAtDistance(currentActiveUnit.Location, distance))
    //         {
    //             if (cell != null && currentActiveUnit.CanMoveTo(cell))
    //             {
    //                 float score = CalculateCellScore(cell, targetCell);
    //                 if (score > bestScore)
    //                 {
    //                     bestScore = score;
    //                     bestCell = cell;
    //                 }
    //             }
    //         }
    //     }

    //     return bestCell;
    // }


    // private float CalculateCellScore(HexCell cell, HexCell targetCell)
    // {
    //     // Оцениваем клетку по расстоянию до цели
    //     float distanceScore = 1f / (1f + HexDistance(cell, targetCell));
        
    //     return distanceScore;
    // }

    // private bool CanAttackTarget(BattleHexUnit target)
    // {
    //     // Проверяем расстояние для атаки (пока ближний бой - соседние клетки)
    //     return HexDistance(currentActiveUnit.Location, target.Location) <= 1;
    // }

    // Метод для вызова из UI или системы ввода
    public void PlayerUnitFinishedTurn()
    {
        if (isPlayerTurn && currentActiveUnit != null)
        {
            EndCurrentUnitTurn();
        }
    }

    // private List<HexCell> GetCellsAtDistance(HexCell startCell, int distance)
    // {
    //     List<HexCell> result = new List<HexCell>();

    //     // Простая реализация - получаем все клетки и фильтруем по расстоянию
    //     HexCell[] allCells = FindObjectsOfType<HexCell>();
    //     foreach (HexCell cell in allCells)
    //     {
    //         if (HexDistance(startCell, cell) == distance)
    //         {
    //             result.Add(cell);
    //         }
    //     }

    //     return result;
    // }

    // private int HexDistance(HexCell a, HexCell b)
    // {
    //     if (a == null || b == null) return int.MaxValue;

    //     HexCoordinates coordA = a.coordinates;
    //     HexCoordinates coordB = b.coordinates;

    //     return ((Mathf.Abs(coordA.X - coordB.X) +
    //             Mathf.Abs(coordA.Y - coordB.Y) +
    //             Mathf.Abs(coordA.Z - coordB.Z)) / 2);
    // }

}