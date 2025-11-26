using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Управляет проверкой условий победы/поражения в битве
/// </summary>
public class BattleVictoryManager : MonoBehaviour
{
    public static BattleVictoryManager Instance { get; private set; }

    private BattleConfig battleConfig;
    private BattleTurnManager turnManager;
    private bool battleEnded = false;

    // Данные о результате битвы для передачи в сцену завершения
    public static BattleResult LastBattleResult { get; private set; }

    /// <summary>
    /// Очищает результат битвы (вызывается при возврате в главное меню)
    /// </summary>
    public static void ClearBattleResult()
    {
        LastBattleResult = null;
    }

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
        battleConfig = BattleSettingsManager.CurrentConfig;
        if (battleConfig == null)
        {
            battleConfig = new BattleConfig();
            Debug.LogWarning("BattleConfig не найден, использую настройки по умолчанию");
        }

        turnManager = BattleTurnManager.Instance;
        if (turnManager == null)
        {
            Debug.LogError("BattleTurnManager не найден!");
            return;
        }

        // Подписываемся на события смерти всех юнитов с небольшой задержкой,
        // чтобы убедиться, что все юниты созданы
        StartCoroutine(SubscribeToUnitDeathsDelayed());
    }

    private System.Collections.IEnumerator SubscribeToUnitDeathsDelayed()
    {
        // Ждем, пока все юниты будут созданы
        yield return new WaitForSeconds(1f);
        SubscribeToUnitDeaths();
    }

    /// <summary>
    /// Подписывается на события смерти всех существующих и будущих юнитов
    /// </summary>
    private void SubscribeToUnitDeaths()
    {
        // Подписываемся на существующие юниты
        BattleHexUnit[] allUnits = FindObjectsByType<BattleHexUnit>(FindObjectsSortMode.None);
        foreach (BattleHexUnit unit in allUnits)
        {
            if (unit != null)
            {
                // Отписываемся перед подпиской, чтобы избежать дублирования
                unit.OnUnitDied -= OnUnitDied;
                unit.OnUnitDied += OnUnitDied;
            }
        }

        Debug.Log($"Подписался на события смерти {allUnits.Length} юнитов");

        // Также подписываемся на новые юниты через BattleTurnManager
        if (turnManager != null)
        {
            turnManager.OnUnitsInitialized += OnUnitsInitialized;
        }
    }

    private void OnUnitsInitialized()
    {
        // Переподписываемся на все юниты после инициализации
        BattleHexUnit[] allUnits = FindObjectsByType<BattleHexUnit>(FindObjectsSortMode.None);
        foreach (BattleHexUnit unit in allUnits)
        {
            // Отписываемся перед подпиской, чтобы избежать дублирования
            unit.OnUnitDied -= OnUnitDied;
            unit.OnUnitDied += OnUnitDied;
        }
    }

    /// <summary>
    /// Обрабатывает смерть юнита и проверяет условия победы
    /// </summary>
    private void OnUnitDied(BattleHexUnit deadUnit)
    {
        if (battleEnded)
            return;

        Debug.Log($"Юнит {deadUnit.name} погиб. Проверяю условия победы...");

        // Проверяем условия победы
        CheckVictoryConditions();
    }

    /// <summary>
    /// Проверяет условия победы в зависимости от выбранного режима
    /// </summary>
    private void CheckVictoryConditions()
    {
        if (battleConfig == null || turnManager == null)
            return;

        switch (battleConfig.victoryCondition)
        {
            case VictoryCondition.TotalAnnihilation:
                CheckTotalAnnihilation();
                break;
            default:
                Debug.LogWarning($"Неизвестный режим победы: {battleConfig.victoryCondition}");
                break;
        }
    }

    /// <summary>
    /// Проверяет условие "Полное уничтожение"
    /// </summary>
    private void CheckTotalAnnihilation()
    {
        List<BattleHexUnit> player1Units = turnManager.GetPlayer1Units();
        List<BattleHexUnit> player2Units = turnManager.GetPlayer2Units();

        // Проверяем, есть ли живые юниты в командах (не изменяем оригинальные списки)
        bool player1HasAliveUnits = false;
        bool player2HasAliveUnits = false;

        foreach (BattleHexUnit unit in player1Units)
        {
            if (unit != null && unit.IsAlive)
            {
                player1HasAliveUnits = true;
                break;
            }
        }

        foreach (BattleHexUnit unit in player2Units)
        {
            if (unit != null && unit.IsAlive)
            {
                player2HasAliveUnits = true;
                break;
            }
        }

        if (!player1HasAliveUnits && !player2HasAliveUnits)
        {
            // Ничья (все погибли одновременно)
            EndBattle(null, "Все юниты уничтожены");
        }
        else if (!player1HasAliveUnits)
        {
            // Победил игрок 2
            EndBattle(false, "Команда противника уничтожена");
        }
        else if (!player2HasAliveUnits)
        {
            // Победил игрок 1
            EndBattle(true, "Команда противника уничтожена");
        }
    }

    /// <summary>
    /// Завершает битву и переходит на экран завершения
    /// </summary>
    /// <param name="player1Won">true если победил игрок 1, false если игрок 2, null если ничья</param>
    /// <param name="victoryConditionText">Текст условия победы</param>
    private void EndBattle(bool? player1Won, string victoryConditionText)
    {
        if (battleEnded)
            return;

        battleEnded = true;

        // Определяем тип управления победителя
        ControlType winnerControlType = ControlType.Computer;
        string winnerName = "Игрок 2";
        
        if (player1Won == true)
        {
            winnerName = "Игрок 1";
            winnerControlType = battleConfig.player1ControlType;
        }
        else if (player1Won == false)
        {
            winnerName = "Игрок 2";
            winnerControlType = battleConfig.player2ControlType;
        }
        else
        {
            // Ничья
            winnerName = "Ничья";
            winnerControlType = ControlType.Computer;
        }

        // Сохраняем результат битвы
        LastBattleResult = new BattleResult
        {
            player1Won = player1Won,
            winnerName = winnerName,
            winnerControlType = winnerControlType,
            victoryConditionText = victoryConditionText
        };

        Debug.Log($"Битва завершена! Победитель: {winnerName} ({winnerControlType})");

        // Переходим на экран завершения битвы
        SceneManager.LoadScene("BattleEnd");
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (turnManager != null)
        {
            turnManager.OnUnitsInitialized -= OnUnitsInitialized;
        }

        BattleHexUnit[] allUnits = FindObjectsByType<BattleHexUnit>(FindObjectsSortMode.None);
        foreach (BattleHexUnit unit in allUnits)
        {
            unit.OnUnitDied -= OnUnitDied;
        }
    }
}

/// <summary>
/// Результат битвы для передачи в сцену завершения
/// </summary>
[System.Serializable]
public class BattleResult
{
    public bool? player1Won; // true = игрок 1, false = игрок 2, null = ничья
    public string winnerName;
    public ControlType winnerControlType;
    public string victoryConditionText;
}

