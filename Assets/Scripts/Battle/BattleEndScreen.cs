using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Управляет экраном завершения битвы
/// </summary>
public class BattleEndScreen : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI victoryConditionText;
    [SerializeField] private Button mainMenuButton;

    void Start()
    {
        // Инициализируем кнопку
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        // Отображаем результат битвы
        DisplayBattleResult();
    }

    /// <summary>
    /// Отображает результат битвы на экране
    /// </summary>
    private void DisplayBattleResult()
    {
        BattleResult result = BattleVictoryManager.LastBattleResult;

        if (result == null)
        {
            Debug.LogWarning("Результат битвы не найден!");
            if (winnerText != null)
                winnerText.text = "Результат не найден";
            if (victoryConditionText != null)
                victoryConditionText.text = "";
            return;
        }

        // Формируем текст победителя
        string winnerDisplayText = "";
        if (result.player1Won == null)
        {
            // Ничья
            winnerDisplayText = "Ничья";
        }
        else
        {
            string controlTypeText = result.winnerControlType == ControlType.Human ? "Человек" : "Компьютер";
            winnerDisplayText = $"{result.winnerName} ({controlTypeText}) Победил";
        }

        // Отображаем текст
        if (winnerText != null)
        {
            winnerText.text = winnerDisplayText;
        }

        if (victoryConditionText != null)
        {
            victoryConditionText.text = result.victoryConditionText;
        }

        Debug.Log($"Отображаю результат: {winnerDisplayText}, Условие: {result.victoryConditionText}");
    }

    /// <summary>
    /// Возвращает в главное меню
    /// </summary>
    private void ReturnToMainMenu()
    {
        // Очищаем результат битвы
        BattleVictoryManager.ClearBattleResult();
        BattleSettingsManager.ClearConfig();

        SceneManager.LoadScene("MainMenu");
    }
}


