using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class BattleSettingsManager : MonoBehaviour
{
    [Header("Map Size Settings")]
    [SerializeField] private Button smallMapButton;
    [SerializeField] private Button mediumMapButton;
    [SerializeField] private Button largeMapButton;

    [Header("Units Count Settings")]
    [SerializeField] private Slider playerUnitsSlider;
    [SerializeField] private Slider enemyUnitsSlider;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text enemyCountText;

    [Header("Battle Settings")]
    [SerializeField] private Button startBattleButton;
    [SerializeField] private Button backButton;

    // Данные для передачи в сцену битвы
    public static BattleConfig CurrentConfig { get; private set; }

    void Start()
    {
        InitializeMapSizeButtons();
        InitializeSliders();
        InitializeActionButtons();

        // Установим значения по умолчанию
        SetMapSize(MapSize.Medium);
        UpdatePlayerUnitsCount();
        UpdateEnemyUnitsCount();
    }

    private void InitializeMapSizeButtons()
    {
        smallMapButton.onClick.AddListener(() => SetMapSize(MapSize.Small));
        mediumMapButton.onClick.AddListener(() => SetMapSize(MapSize.Medium));
        largeMapButton.onClick.AddListener(() => SetMapSize(MapSize.Large));
    }

    private void InitializeSliders()
    {
        playerUnitsSlider.onValueChanged.AddListener((value) => UpdatePlayerUnitsCount());
        enemyUnitsSlider.onValueChanged.AddListener((value) => UpdateEnemyUnitsCount());

        // Настройка слайдеров
        playerUnitsSlider.minValue = 1;
        playerUnitsSlider.maxValue = 6;
        playerUnitsSlider.value = 3;

        enemyUnitsSlider.minValue = 1;
        enemyUnitsSlider.maxValue = 6;
        enemyUnitsSlider.value = 3;
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

    private void UpdatePlayerUnitsCount()
    {
        int count = (int)playerUnitsSlider.value;
        playerCountText.text = count.ToString();

        if (CurrentConfig == null)
            CurrentConfig = new BattleConfig();

        CurrentConfig.playerUnitsCount = count;
    }

    private void UpdateEnemyUnitsCount()
    {
        int count = (int)enemyUnitsSlider.value;
        enemyCountText.text = count.ToString();

        if (CurrentConfig == null)
            CurrentConfig = new BattleConfig();

        CurrentConfig.enemyUnitsCount = count;
    }

    private void StartBattle()
    {
        Debug.Log("Запуск битвы с настройками:");
        Debug.Log($"Размер карты: {CurrentConfig.mapSize}");
        Debug.Log($"Юнитов игрока: {CurrentConfig.playerUnitsCount}");
        Debug.Log($"Юнитов врага: {CurrentConfig.enemyUnitsCount}");

        // Здесь будет загрузка сцены битвы
        // SceneManager.LoadScene("BattleScene");

        // Временно выведем сообщение - исправленная версия для TMPro
        startBattleButton.GetComponentInChildren<TMP_Text>().text = "Сцена битвы в разработке!";
    }

    private void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
