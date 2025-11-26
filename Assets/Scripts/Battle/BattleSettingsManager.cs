using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class BattleSettingsManager : MonoBehaviour
{
    [Header("Map Size Settings")]
    [SerializeField] private Button smallMapButton;
    [SerializeField] private Button mediumMapButton;
    [SerializeField] private Button largeMapButton;

    [Header("Player 1 Settings")]
    [SerializeField] private TMP_Dropdown player1ControlTypeDropdown;
    [SerializeField] private TMP_Dropdown[] player1UnitDropdowns = new TMP_Dropdown[6];

    [Header("Player 2 Settings")]
    [SerializeField] private TMP_Dropdown player2ControlTypeDropdown;
    [SerializeField] private TMP_Dropdown[] player2UnitDropdowns = new TMP_Dropdown[6];

    [Header("Battle Settings")]
    [SerializeField] private TMP_Dropdown victoryConditionDropdown;
    [SerializeField] private Button startBattleButton;
    [SerializeField] private Button backButton;

    // Список доступных юнитов (загружается из Resources)
    private List<UnitData> availableUnits = new List<UnitData>();

    // Данные для передачи в сцену битвы
    public static BattleConfig CurrentConfig { get; private set; }

    /// <summary>
    /// Очищает текущую конфигурацию битвы (вызывается при возврате в главное меню)
    /// </summary>
    public static void ClearConfig()
    {
        CurrentConfig = null;
    }

    void Start()
    {
        LoadAvailableUnits();
        InitializeMapSizeButtons();
        InitializeControlTypeDropdowns();
        InitializeUnitDropdowns();
        InitializeVictoryConditionDropdown();
        InitializeActionButtons();

        // Установим значения по умолчанию
        if (CurrentConfig == null)
            CurrentConfig = new BattleConfig();

        SetMapSize(MapSize.Medium);
        ApplyDefaultSettings();
    }

    private void LoadAvailableUnits()
    {
        // Загружаем все UnitData из Resources/UnitData
        UnitData[] units = Resources.LoadAll<UnitData>("UnitData");
        availableUnits.Clear();
        availableUnits.AddRange(units);

        Debug.Log($"Загружено {availableUnits.Count} доступных юнитов");
    }

    private void InitializeMapSizeButtons()
    {
        smallMapButton.onClick.AddListener(() => SetMapSize(MapSize.Small));
        mediumMapButton.onClick.AddListener(() => SetMapSize(MapSize.Medium));
        largeMapButton.onClick.AddListener(() => SetMapSize(MapSize.Large));
    }

    private void InitializeControlTypeDropdowns()
    {
        // Настраиваем выпадающие списки типа управления
        List<string> controlTypeOptions = new List<string> { "Человек", "ИИ" };

        if (player1ControlTypeDropdown != null)
        {
            player1ControlTypeDropdown.ClearOptions();
            player1ControlTypeDropdown.AddOptions(controlTypeOptions);
            player1ControlTypeDropdown.value = 0; // По умолчанию "Человек"
            player1ControlTypeDropdown.onValueChanged.AddListener((value) => OnPlayer1ControlTypeChanged(value));
        }

        if (player2ControlTypeDropdown != null)
        {
            player2ControlTypeDropdown.ClearOptions();
            player2ControlTypeDropdown.AddOptions(controlTypeOptions);
            player2ControlTypeDropdown.value = 1; // По умолчанию "ИИ"
            player2ControlTypeDropdown.onValueChanged.AddListener((value) => OnPlayer2ControlTypeChanged(value));
        }
    }

    private void InitializeUnitDropdowns()
    {
        // Создаем список опций для выбора юнитов
        // Первый элемент - прочерк "-", затем идут все доступные юниты
        List<string> unitOptions = new List<string> { "-" };
        foreach (UnitData unit in availableUnits)
        {
            string unitName = string.IsNullOrEmpty(unit.unitName) ? unit.name : unit.unitName;
            unitOptions.Add(unitName);
        }

        // Настраиваем dropdown'ы для Игрока 1
        for (int i = 0; i < player1UnitDropdowns.Length; i++)
        {
            if (player1UnitDropdowns[i] != null)
            {
                player1UnitDropdowns[i].ClearOptions();
                player1UnitDropdowns[i].AddOptions(unitOptions);

                // Первый юнит обязателен (не может быть прочерком)
                if (i == 0)
                {
                    player1UnitDropdowns[i].value = availableUnits.Count > 0 ? 1 : 0; // Выбираем первого доступного юнита
                }
                else
                {
                    player1UnitDropdowns[i].value = 0; // Остальные по умолчанию прочерк "-"
                }

                int index = i; // Захватываем индекс для лямбды
                player1UnitDropdowns[i].onValueChanged.AddListener((value) => OnPlayer1UnitChanged(index, value));
            }
        }

        // Настраиваем dropdown'ы для Игрока 2
        for (int i = 0; i < player2UnitDropdowns.Length; i++)
        {
            if (player2UnitDropdowns[i] != null)
            {
                player2UnitDropdowns[i].ClearOptions();
                player2UnitDropdowns[i].AddOptions(unitOptions);

                // Первый юнит обязателен (не может быть прочерком)
                if (i == 0)
                {
                    player2UnitDropdowns[i].value = availableUnits.Count > 0 ? 1 : 0; // Выбираем первого доступного юнита
                }
                else
                {
                    player2UnitDropdowns[i].value = 0; // Остальные по умолчанию прочерк "-"
                }

                int index = i; // Захватываем индекс для лямбды
                player2UnitDropdowns[i].onValueChanged.AddListener((value) => OnPlayer2UnitChanged(index, value));
            }
        }
    }

    private void InitializeVictoryConditionDropdown()
    {
        if (victoryConditionDropdown != null)
        {
            List<string> victoryConditionOptions = new List<string> { "Полное уничтожение" };
            victoryConditionDropdown.ClearOptions();
            victoryConditionDropdown.AddOptions(victoryConditionOptions);
            victoryConditionDropdown.value = 0; // По умолчанию "Полное уничтожение"
            victoryConditionDropdown.onValueChanged.AddListener((value) => OnVictoryConditionChanged(value));
        }
    }

    private void OnVictoryConditionChanged(int value)
    {
        if (CurrentConfig == null)
            CurrentConfig = new BattleConfig();

        CurrentConfig.victoryCondition = (VictoryCondition)value;
        Debug.Log($"Режим победы: {CurrentConfig.victoryCondition}");
    }

    private void InitializeActionButtons()
    {
        startBattleButton.onClick.AddListener(StartBattle);
        backButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void SetMapSize(MapSize size)
    {
        // Сбрасываем выделение всех кнопок
        ResetMapSizeButtons();

        // Выделяем выбранную кнопку
        Button selectedButton = size switch
        {
            MapSize.Small => smallMapButton,
            MapSize.Medium => mediumMapButton,
            MapSize.Large => largeMapButton,
            _ => mediumMapButton
        };

        // Визуально выделяем выбранную кнопку через выделенный цвет
        ColorBlock colors = selectedButton.colors;
        colors.selectedColor = Color.green;
        colors.normalColor = Color.green; // Основной цвет тоже зеленый
        selectedButton.colors = colors;

        // Принудительно делаем кнопку "выбранной"
        selectedButton.Select();

        // Сохраняем настройки
        if (CurrentConfig == null)
            CurrentConfig = new BattleConfig();

        CurrentConfig.mapSize = size;

        Debug.Log($"Выбран размер карты: {size}");
    }

    private void ResetMapSizeButtons()
    {
        Button[] mapButtons = { smallMapButton, mediumMapButton, largeMapButton };

        foreach (Button button in mapButtons)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.selectedColor = Color.white;
            button.colors = colors;
        }
    }

    private void OnPlayer1ControlTypeChanged(int value)
    {
        if (CurrentConfig == null)
            CurrentConfig = new BattleConfig();

        CurrentConfig.player1ControlType = value == 0 ? ControlType.Human : ControlType.Computer;
        Debug.Log($"Игрок 1: {CurrentConfig.player1ControlType}");
    }

    private void OnPlayer2ControlTypeChanged(int value)
    {
        if (CurrentConfig == null)
            CurrentConfig = new BattleConfig();

        CurrentConfig.player2ControlType = value == 0 ? ControlType.Human : ControlType.Computer;
        Debug.Log($"Игрок 2: {CurrentConfig.player2ControlType}");
    }

    private void OnPlayer1UnitChanged(int dropdownIndex, int value)
    {
        if (CurrentConfig == null)
            CurrentConfig = new BattleConfig();

        // Первый юнит обязателен - не разрешаем выбор прочерка
        if (dropdownIndex == 0 && value == 0)
        {
            // Возвращаем выбранное значение обратно к первому доступному юниту
            if (player1UnitDropdowns[0] != null && availableUnits.Count > 0)
            {
                player1UnitDropdowns[0].value = 1;
                return;
            }
        }

        UpdatePlayerUnits(CurrentConfig.player1Units, player1UnitDropdowns);
        Debug.Log($"Игрок 1: выбрано {CurrentConfig.player1Units.Count} юнитов");
    }

    private void OnPlayer2UnitChanged(int dropdownIndex, int value)
    {
        if (CurrentConfig == null)
            CurrentConfig = new BattleConfig();

        // Первый юнит обязателен - не разрешаем выбор прочерка
        if (dropdownIndex == 0 && value == 0)
        {
            // Возвращаем выбранное значение обратно к первому доступному юниту
            if (player2UnitDropdowns[0] != null && availableUnits.Count > 0)
            {
                player2UnitDropdowns[0].value = 1;
                return;
            }
        }

        UpdatePlayerUnits(CurrentConfig.player2Units, player2UnitDropdowns);
        Debug.Log($"Игрок 2: выбрано {CurrentConfig.player2Units.Count} юнитов");
    }

    private void UpdatePlayerUnits(List<UnitData> unitsList, TMP_Dropdown[] dropdowns)
    {
        unitsList.Clear();

        foreach (TMP_Dropdown dropdown in dropdowns)
        {
            if (dropdown != null)
            {
                int selectedIndex = dropdown.value;
                // Индекс 0 = прочерк "-", индекс 1+ = юниты из availableUnits
                if (selectedIndex > 0 && selectedIndex <= availableUnits.Count)
                {
                    int unitIndex = selectedIndex - 1; // Конвертируем в индекс массива availableUnits
                    unitsList.Add(availableUnits[unitIndex]);
                }
            }
        }
    }

    private void ApplyDefaultSettings()
    {
        // Применяем настройки по умолчанию
        if (player1ControlTypeDropdown != null)
            OnPlayer1ControlTypeChanged(player1ControlTypeDropdown.value);

        if (player2ControlTypeDropdown != null)
            OnPlayer2ControlTypeChanged(player2ControlTypeDropdown.value);

        if (victoryConditionDropdown != null)
            OnVictoryConditionChanged(victoryConditionDropdown.value);

        // Применяем выбранные юниты
        if (player1UnitDropdowns[0] != null)
            OnPlayer1UnitChanged(0, player1UnitDropdowns[0].value);

        if (player2UnitDropdowns[0] != null)
            OnPlayer2UnitChanged(0, player2UnitDropdowns[0].value);
    }

    private void StartBattle()
    {
        if (CurrentConfig == null)
            CurrentConfig = new BattleConfig();

        // Обновляем списки юнитов перед запуском
        UpdatePlayerUnits(CurrentConfig.player1Units, player1UnitDropdowns);
        UpdatePlayerUnits(CurrentConfig.player2Units, player2UnitDropdowns);

        // Проверяем что у каждого игрока есть хотя бы один юнит
        if (CurrentConfig.player1Units.Count == 0)
        {
            Debug.LogError("Игрок 1 должен выбрать хотя бы одного юнита!");
            return;
        }

        if (CurrentConfig.player2Units.Count == 0)
        {
            Debug.LogError("Игрок 2 должен выбрать хотя бы одного юнита!");
            return;
        }

        Debug.Log("Запуск битвы с настройками:");
        Debug.Log($"Размер карты: {CurrentConfig.mapSize}");
        Debug.Log($"Игрок 1: {CurrentConfig.player1ControlType}, {CurrentConfig.player1Units.Count} юнитов");
        Debug.Log($"Игрок 2: {CurrentConfig.player2ControlType}, {CurrentConfig.player2Units.Count} юнитов");

        // Загружаем сцену битвы
        SceneManager.LoadScene("Battle");
    }

    private void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
