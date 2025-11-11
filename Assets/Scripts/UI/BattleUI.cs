using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class BattleUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform unitButtonsPanel;
    [SerializeField] private GameObject unitButtonPrefab;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private Button endTurnButton;
	[SerializeField] BattleHexGrid grid;

    private List<Button> unitButtons = new List<Button>();
    private BattleTurnManager turnManager;
    private BattleHexUnit selectedUnit;

	HexCell currentCell;

    void Start()
    {
        turnManager = BattleTurnManager.Instance;

        if (turnManager == null)
        {
            Debug.LogError("TurnManager не найден!");
            return;
        }

        // Подписываемся на события
        turnManager.OnUnitsInitialized += InitializeUnitButtons;
        turnManager.OnActiveUnitChanged += OnActiveUnitChanged;

        endTurnButton.onClick.AddListener(OnEndTurnClick);

        // Инициализируем UI в выключенном состоянии
        SetUnitInfoVisible(false);
    }
    
    void Update()
    {
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			if (selectedUnit != null && selectedUnit.CompareTag("PlayerUnit"))
			{
				if (Input.GetMouseButtonDown(0))
				{
					DoMove();
				}
				else
                {
					DoPathfinding();
				}
			}
            else
            {
                ClearPath();
            }
		} else
        {
            ClearPath();
        }
	}

    void OnDestroy()
    {
        // Отписываемся от событий при уничтожении
        if (turnManager != null)
        {
            turnManager.OnUnitsInitialized -= InitializeUnitButtons;
            turnManager.OnActiveUnitChanged -= OnActiveUnitChanged;
        }
        
        // Отписываемся от событий юнитов
        UnsubscribeFromUnitEvents();
    }

    private void InitializeUnitButtons()
    {
        // Очищаем панель
        foreach (Transform child in unitButtonsPanel)
        {
            Destroy(child.gameObject);
        }
        unitButtons.Clear();

        // Получаем юнитов игрока
        var playerUnits = turnManager.GetPlayerUnits();
        
        for (int i = 0; i < playerUnits.Count; i++)
        {
            CreateUnitButton(i, playerUnits[i]);
        }
        
        Debug.Log($"Создано {playerUnits.Count} кнопок юнитов");
    }

    private void CreateUnitButton(int index, BattleHexUnit unit)
    {
        GameObject buttonGO = Instantiate(unitButtonPrefab, unitButtonsPanel);
        Button button = buttonGO.GetComponent<Button>();
        TMP_Text buttonText = buttonGO.GetComponentInChildren<TMP_Text>();
        
        buttonText.text = (index + 1).ToString();
        
        // Назначаем обработчик клика
        int unitIndex = index;
        button.onClick.AddListener(() => OnUnitButtonClick(unitIndex));
        
        unitButtons.Add(button);
        
        // Подписываемся на события юнита для обновления кнопки
        unit.OnHealthChanged += (u) => UpdateUnitButtonVisual(button, u);
        unit.OnUnitDied += (u) => OnUnitDied(u, button);
    }

    private void OnActiveUnitChanged(BattleHexUnit unit)
    {
        
        // Отписываемся от предыдущего юнита
        UnsubscribeFromUnitEvents();

        if (unit == null)
        {
            selectedUnit = null;
            return;
        }


        // Зарнуляем прошлое выделение
        if (selectedUnit != null)
        {
            grid.DisableHighlight(selectedUnit.Location.Index);
        } 

        // Подписываемся на нового активного юнита
        selectedUnit = unit;
        unit.OnStaminaChanged += OnStaminaChanged;
        unit.OnHealthChanged += OnHealthChanged;
        
        // Обновляем UI
        UpdateUnitInfo(unit);
        SetUnitInfoVisible(true);
        
        // Подсвечиваем активную кнопку
        UpdateUnitButtonsSelection(unit);
    }

    private void OnStaminaChanged(BattleHexUnit unit)
    {
        if (unit == selectedUnit)
        {
            UpdateStaminaDisplay(unit);
        }
    }

    private void OnHealthChanged(BattleHexUnit unit)
    {
        if (unit == selectedUnit)
        {
            UpdateUnitInfo(unit);
        }
    }

    private void UpdateUnitInfo(BattleHexUnit unit)
    {
        if (unit != null && unit.IsAlive)
        {
            unitNameText.text = unit.name;
            UpdateStaminaDisplay(unit);
        }
        else
        {
            SetUnitInfoVisible(false);
        }
    }

    private void UpdateStaminaDisplay(BattleHexUnit unit)
    {
        staminaText.text = $"Stamina: {unit.currentStamina}/{unit.maxStamina}";
    }

    private void UpdateUnitButtonsSelection(BattleHexUnit selectedUnit)
    {
        var playerUnits = turnManager.GetPlayerUnits();
        int selectedIndex = playerUnits.IndexOf(selectedUnit);
        
        for (int i = 0; i < unitButtons.Count; i++)
        {
            Image buttonImage = unitButtons[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                // Подсвечиваем выбранную кнопку
                buttonImage.color = (i == selectedIndex) ? Color.green : Color.white;
            }
        }
    }

    private void UpdateUnitButtonVisual(Button button, BattleHexUnit unit)
    {
        // Меняем визуал кнопки в зависимости от состояния юнита
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            if (!unit.IsAlive)
            {
                buttonImage.color = Color.gray;
                button.interactable = false;
            }
            else if (unit.currentHealth < unit.maxHealth * 0.3f)
            {
                buttonImage.color = Color.red;
            }
        }
    }

    private void OnUnitDied(BattleHexUnit unit, Button button)
    {
        UpdateUnitButtonVisual(button, unit);
        
        // Если умер текущий отображаемый юнит, скрываем информацию
        if (unit == selectedUnit)
        {
            SetUnitInfoVisible(false);
            selectedUnit = null;
        }
    }

    private void SetUnitInfoVisible(bool visible)
    {
        staminaText.gameObject.SetActive(visible);
        unitNameText.gameObject.SetActive(visible);
        
        if (!visible)
        {
            staminaText.text = "Stamina: -/-";
            unitNameText.text = "No unit selected";
        }
    }

    private void UnsubscribeFromUnitEvents()
    {
        if (selectedUnit != null)
        {
            selectedUnit.OnStaminaChanged -= OnStaminaChanged;
            selectedUnit.OnHealthChanged -= OnHealthChanged;
        }
    }

    private void OnUnitButtonClick(int unitIndex)
    {
        turnManager.SelectUnit(unitIndex);
    }

    private void OnEndTurnClick()
    {
        turnManager.PlayerUnitFinishedTurn();
    }

    void DoPathfinding()
    {
        if (UpdateCurrentCell())
        {
            if (currentCell && selectedUnit.IsValidDestination(currentCell))
            {
                grid.FindPath(selectedUnit.Location, currentCell, selectedUnit);
            }
            else
            {
                ClearPath();
            }
        }
        else
        {
            ClearPath();
        }
    }
    
    void ClearPath()
    {
        grid.ClearPath();
        if (selectedUnit != null && selectedUnit.CompareTag("PlayerUnit"))
        {
            grid.HighlightUnitCell(selectedUnit.Location.Index);        
        }
    
    }

    void DoMove()
    {
        if (grid.HasPath)
        {
            if (grid.PathIsReachable)
            {
                selectedUnit.BattleMoveTo();
                ClearPath();
            } else
            {
                Debug.Log("Is not Reachable");
            }
        }
    }
    
    bool UpdateCurrentCell()
    {
        // Проверяем что grid и камера не null
        if (grid == null)
        {
            Debug.LogError("Grid is not assigned!");
            return false;
        }
        
        if (Camera.main == null)
        {
            Debug.LogError("Main camera is not found!");
            return false;
        }

        HexCell cell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        
		if (cell)
		{
			currentCell = cell;
			return true;
		}
		return false;
	}
}