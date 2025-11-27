using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public enum ActionMode
{
    Move,
    Action
}

public class BattleUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform unitButtonsPanel;
    [SerializeField] private GameObject unitButtonPrefab;
    [SerializeField] private Transform skillButtonsPanel;
    [SerializeField] private GameObject skillButtonPrefab;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private Button endTurnButton;
    [SerializeField] BattleHexGrid grid;

    private List<Button> unitButtons = new List<Button>();
    private List<Button> skillButtons = new List<Button>();
    private Dictionary<BattleHexUnit, Button> unitButtonMap = new Dictionary<BattleHexUnit, Button>();
    private Dictionary<BattleHexUnit, System.Action<BattleHexUnit>> unitHealthChangedHandlers = new Dictionary<BattleHexUnit, System.Action<BattleHexUnit>>();
    private Dictionary<BattleHexUnit, System.Action<BattleHexUnit>> unitDiedHandlers = new Dictionary<BattleHexUnit, System.Action<BattleHexUnit>>();
    private BattleTurnManager turnManager;
    private BattleHexUnit selectedUnit;
    private List<BattleHexUnit> cachedHumanControlledUnits = new List<BattleHexUnit>();

    HexCell currentCell;
    
    // Режим действия: Move или Action
    private ActionMode currentMode = ActionMode.Move;
    // Выбранный навык (null если режим Move)
    private AbstractBattleSkill selectedSkill = null;

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
        turnManager.OnTurnChanged += OnTurnChanged;

        endTurnButton.onClick.AddListener(OnEndTurnClick);

        // Инициализируем UI в выключенном состоянии
        SetUnitInfoVisible(false);
    }
    
    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            ClearAllPreviews();
            return;
        }

        if (!IsHumanControlledUnitSelected())
        {
            ClearAllPreviews();
            return;
        }

        HandleInput();
    }

    private void HandleInput()
    {
        if (currentMode == ActionMode.Move)
        {
            HandleMoveInput();
        }
        else if (currentMode == ActionMode.Action && selectedSkill != null)
        {
            HandleActionInput();
        }
    }

    private void HandleMoveInput()
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

    private void HandleActionInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            DoAction();
        }
        else
        {
            DoActionTargeting();
        }
    }

    private bool IsHumanControlledUnitSelected()
    {
        if (selectedUnit == null || turnManager == null)
            return false;

        // Используем кэшированный список для производительности
        if (cachedHumanControlledUnits == null || cachedHumanControlledUnits.Count == 0)
        {
            UpdateCachedHumanControlledUnits();
        }

        return cachedHumanControlledUnits.Contains(selectedUnit);
    }

    private void UpdateCachedHumanControlledUnits()
    {
        if (turnManager != null)
        {
            cachedHumanControlledUnits = turnManager.GetHumanControlledUnits();
        }
    }

    private void ClearAllPreviews()
    {
        ClearPath();
        ClearActionTargeting();
    }

    void OnDestroy()
    {
        // Отписываемся от событий при уничтожении
        if (turnManager != null)
        {
            turnManager.OnUnitsInitialized -= InitializeUnitButtons;
            turnManager.OnActiveUnitChanged -= OnActiveUnitChanged;
            turnManager.OnTurnChanged -= OnTurnChanged;
        }
        
        // Отписываемся от событий всех юнитов, связанных с кнопками
        foreach (var kvp in unitHealthChangedHandlers)
        {
            if (kvp.Key != null)
            {
                kvp.Key.OnHealthChanged -= kvp.Value;
            }
        }
        foreach (var kvp in unitDiedHandlers)
        {
            if (kvp.Key != null)
            {
                kvp.Key.OnUnitDied -= kvp.Value;
            }
        }
        
        // Отписываемся от событий выбранного юнита
        UnsubscribeFromUnitEvents();
    }

    private void InitializeUnitButtons()
    {
        // Отписываемся от событий всех юнитов перед уничтожением кнопок
        foreach (var kvp in unitHealthChangedHandlers)
        {
            if (kvp.Key != null)
            {
                kvp.Key.OnHealthChanged -= kvp.Value;
            }
        }
        foreach (var kvp in unitDiedHandlers)
        {
            if (kvp.Key != null)
            {
                kvp.Key.OnUnitDied -= kvp.Value;
            }
        }
        
        // Очищаем словари
        unitHealthChangedHandlers.Clear();
        unitDiedHandlers.Clear();
        
        // Очищаем панель
        foreach (Transform child in unitButtonsPanel)
        {
            Destroy(child.gameObject);
        }
        unitButtons.Clear();
        unitButtonMap.Clear();

        // Получаем юнитов текущей активной команды, если она управляется человеком
        var currentTeamUnits = turnManager.GetCurrentTeamHumanControlledUnits();
        
        for (int i = 0; i < currentTeamUnits.Count; i++)
        {
            CreateUnitButton(i, currentTeamUnits[i]);
        }
        
        Debug.Log($"Создано {currentTeamUnits.Count} кнопок юнитов текущей команды (управляемых человеком)");
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
        unitButtonMap[unit] = button;
        
        // Создаем обработчики событий и сохраняем ссылки для последующей отписки
        System.Action<BattleHexUnit> healthChangedHandler = (u) => UpdateUnitButtonVisual(button, u);
        System.Action<BattleHexUnit> unitDiedHandler = (u) => OnUnitDied(u, button);
        
        unit.OnHealthChanged += healthChangedHandler;
        unit.OnUnitDied += unitDiedHandler;
        
        // Сохраняем обработчики для последующей отписки
        unitHealthChangedHandlers[unit] = healthChangedHandler;
        unitDiedHandlers[unit] = unitDiedHandler;
    }

    private void InitializeSkillButtons()
    {
        // Очищаем панель
        foreach (Transform child in skillButtonsPanel)
        {
            Destroy(child.gameObject);
        }
        skillButtons.Clear();

        // Если нет выбранного юнита, выходим
        if (selectedUnit == null)
        {
            return;
        }

        // Всегда добавляем кнопку Move в начале
        CreateMoveButton();

        // Получаем навыки выбранного юнита
        if (selectedUnit.Skills != null && selectedUnit.Skills.Count > 0)
        {
            for (int i = 0; i < selectedUnit.Skills.Count; i++)
            {
                if (selectedUnit.Skills[i] != null)
                {
                    CreateSkillButton(i, selectedUnit.Skills[i]);
                }
            }
        }
        
        Debug.Log($"Создано {skillButtons.Count} кнопок навыков для {selectedUnit.name}");
        
        // Обновляем подсветку кнопок после создания всех кнопок
        UpdateSkillButtonsSelection();
    }
    
    private void CreateMoveButton()
    {
        GameObject buttonGO = Instantiate(skillButtonPrefab, skillButtonsPanel);
        Button button = buttonGO.GetComponent<Button>();
        TMP_Text buttonText = buttonGO.GetComponentInChildren<TMP_Text>();
        
        // Выводим название действия
        buttonText.text = "Move";
        
        // Назначаем обработчик клика (индекс -1 для Move)
        button.onClick.AddListener(() => OnSkillButtonClick(-1));

        skillButtons.Add(button);
    }

    private void CreateSkillButton(int skillIndex, AbstractBattleSkill skill)
    {
        GameObject buttonGO = Instantiate(skillButtonPrefab, skillButtonsPanel);
        Button button = buttonGO.GetComponent<Button>();
        TMP_Text buttonText = buttonGO.GetComponentInChildren<TMP_Text>();
        
        // Выводим название навыка
        buttonText.text = skill.skillName;
        
        // Назначаем обработчик клика
        int index = skillIndex;
        button.onClick.AddListener(() => OnSkillButtonClick(index));

        skillButtons.Add(button);
    }

    private void OnActiveUnitChanged(BattleHexUnit unit)
    {
        
        // Отписываемся от предыдущего юнита
        UnsubscribeFromUnitEvents();

        if (unit == null)
        {
            selectedUnit = null;
            // Очищаем кнопки навыков при отсутствии выбранного юнита
            InitializeSkillButtons();
            // Обновляем подсветку кнопок юнитов (убираем подсветку)
            if (unitButtons.Count > 0)
            {
                UpdateUnitButtonsSelection(null);
            }
            return;
        }


        // Сбрасываем прошлое выделение
        if (selectedUnit != null)
        {
            grid.DisableHighlight(selectedUnit.Location.Index);
        } 

        // Подписываемся на нового активного юнита
        selectedUnit = unit;
        unit.OnStaminaChanged += OnStaminaChanged;
        unit.OnHealthChanged += OnHealthChanged;
        
        // Обновляем кэш управляемых юнитов
        UpdateCachedHumanControlledUnits();
        
        // Обновляем UI
        UpdateUnitInfo(unit);
        SetUnitInfoVisible(true);
        
        // Обновляем кнопки навыков для нового выбранного юнита (сначала создаем кнопки)
        InitializeSkillButtons();
        
        // Подсвечиваем активную кнопку (после создания кнопок, если они еще не созданы)
        // Убеждаемся, что кнопки созданы перед обновлением подсветки
        if (unitButtons.Count > 0)
        {
            UpdateUnitButtonsSelection(unit);
        }
        
        // Сбрасываем режим на Move при смене юнита (после создания кнопок, чтобы подсветка работала)
        SetActionMode(ActionMode.Move);
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
        if (unitButtons.Count == 0 || turnManager == null)
        {
            return;
        }
        
        var currentTeamUnits = turnManager.GetCurrentTeamHumanControlledUnits();
        int selectedIndex = selectedUnit != null ? currentTeamUnits.IndexOf(selectedUnit) : -1;
        
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
        // Проверяем, что кнопка не была уничтожена
        if (button == null)
        {
            return;
        }
        
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




    private void UpdateSkillButtonVisual(Button button, BattleHexUnit unit)
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
        // Проверяем, что кнопка не была уничтожена
        if (button != null)
        {
            UpdateUnitButtonVisual(button, unit);
        }
        
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
        // Получаем список юнитов текущей активной команды, управляемой человеком
        var currentTeamUnits = turnManager.GetCurrentTeamHumanControlledUnits();
        
        if (unitIndex >= 0 && unitIndex < currentTeamUnits.Count)
        {
            BattleHexUnit unit = currentTeamUnits[unitIndex];
            
            // Используем метод выбора по юниту напрямую
            turnManager.SelectUnitByUnit(unit);
        }
    }

    private void OnSkillButtonClick(int skillIndex)
    {
        if (selectedUnit == null)
        {
            Debug.LogWarning("Нет выбранного юнита для использования навыка");
            return;
        }

        // Обработка кнопки Move (индекс -1)
        if (skillIndex == -1)
        {
            SetActionMode(ActionMode.Move);
            Debug.Log("Выбран режим перемещения");
            return;
        }

        // Обработка обычных навыков
        if (selectedUnit.Skills == null || skillIndex < 0 || skillIndex >= selectedUnit.Skills.Count)
        {
            Debug.LogWarning($"Неверный индекс навыка: {skillIndex}");
            return;
        }

        AbstractBattleSkill skill = selectedUnit.Skills[skillIndex];
        if (skill == null)
        {
            Debug.LogWarning($"Навык с индексом {skillIndex} не найден");
            return;
        }

        // Переключаемся на режим действия с выбранным навыком
        SetActionMode(ActionMode.Action, skill);
        Debug.Log($"Выбран навык: {skill.skillName} (индекс: {skillIndex})");
    }
    
    private void SetActionMode(ActionMode mode, AbstractBattleSkill skill = null)
    {
        currentMode = mode;
        selectedSkill = skill;
        
        // Очищаем текущее выделение при переключении режима
        ClearPath();
        ClearActionTargeting();
        previousTrajectoryCell = null;
        
        // Обновляем подсветку кнопок
        UpdateSkillButtonsSelection();
    }
    
    private void UpdateSkillButtonsSelection()
    {
        // Подсвечиваем активную кнопку
        for (int i = 0; i < skillButtons.Count; i++)
        {
            Image buttonImage = skillButtons[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                bool isActive = false;
                
                // Кнопка Move (индекс 0)
                if (i == 0 && currentMode == ActionMode.Move)
                {
                    isActive = true;
                }
                // Кнопки навыков (индекс i-1 в массиве Skills, т.к. Move на позиции 0)
                else if (i > 0 && currentMode == ActionMode.Action)
                {
                    int skillIndex = i - 1;
                    if (selectedUnit != null && selectedUnit.Skills != null && 
                        skillIndex < selectedUnit.Skills.Count && 
                        selectedUnit.Skills[skillIndex] == selectedSkill)
                    {
                        isActive = true;
                    }
                }
                
                buttonImage.color = isActive ? Color.green : Color.white;
            }
        }
    }

    private void OnEndTurnClick()
    {
        turnManager.PlayerUnitFinishedTurn();
    }
    
    private void OnTurnChanged(bool isPlayerTurn)
    {
        // При смене хода команды обновляем список кнопок юнитов
        // (важно, когда обе команды управляются человеком)
        InitializeUnitButtons();
        
        // Обновляем кэш управляемых юнитов
        UpdateCachedHumanControlledUnits();
        
        // После пересоздания кнопок обновляем подсветку для текущего выбранного юнита
        if (selectedUnit != null)
        {
            UpdateUnitButtonsSelection(selectedUnit);
        }
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
        if (selectedUnit != null && IsHumanControlledUnitSelected())
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
    
    private HexCell? previousActionTargetCell;
    private HexCell? previousTrajectoryCell;
    
    void DoAction()
    {
        if (selectedSkill == null || selectedUnit == null)
        {
            return;
        }
        
        if (!UpdateCurrentCell() || currentCell == null)
        {
            return;
        }
        
        // Проверяем, является ли клетка валидной целью для навыка
        if (!selectedSkill.IsValidTarget(currentCell, selectedUnit))
        {
            Debug.LogWarning("Недопустимая цель для навыка");
            return;
        }
        
        // Выполняем навык с визуализацией (асинхронно)
        StartCoroutine(ExecuteSkillWithVisualization());
    }

    /// <summary>
    /// Корутина для выполнения навыка с визуализацией
    /// </summary>
    private System.Collections.IEnumerator ExecuteSkillWithVisualization()
    {
        SkillResult result = new SkillResult
        {
            success = false,
            appliedEffects = new List<SkillEffectApplication>()
        };

        // Для ближних атак добавляем задержку 0.1 секунды перед запуском анимаций
        bool isMeleeAttack = selectedSkill is BaseMeleeBattleSkill;
        if (isMeleeAttack)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Проигрываем анимацию атаки перед выполнением навыка
        selectedUnit.PerformAttack();

        // Запускаем корутину визуализации навыка
        yield return StartCoroutine(selectedSkill.ExecuteWithVisualization(
            currentCell,
            selectedUnit,
            (skillResult) =>
            {
                result = skillResult;
            }
        ));

        if (result.success)
        {
            Debug.Log($"Навык {selectedSkill.skillName} успешно применен!");
            
            // Очищаем выделение после успешного использования
            ClearActionTargeting();
            
            // Остаемся в режиме действия с тем же навыком
            // SetActionMode не вызываем, чтобы остался выбранный навык
        }
        else
        {
            Debug.LogWarning($"Не удалось использовать навык {selectedSkill.skillName}");
        }
    }
    
    void DoActionTargeting()
    {
        if (selectedSkill == null || selectedUnit == null)
        {
            ClearActionTargeting();
            return;
        }
        
        // Проверяем, изменилась ли ячейка
        if (!UpdateCurrentCell())
        {
            ClearActionTargeting();
            return;
        }
        
        if (currentCell == null)
        {
            ClearActionTargeting();
            return;
        }
        
        // Выполняем проверки и показываем предпросмотр только если ячейка изменилась
        bool cellChanged = !previousActionTargetCell.HasValue || previousActionTargetCell.Value != currentCell;
        
        if (cellChanged)
        {
            // Очищаем предыдущую подсветку, если клетка изменилась
            if (previousActionTargetCell.HasValue)
            {
                grid.DisableHighlight(previousActionTargetCell.Value.Index);
                selectedSkill.HideTargetingPreview();
                
                // Если предыдущая ячейка была ячейкой активного юнита, подсвечиваем её снова зеленым
                if (previousActionTargetCell.Value == selectedUnit.Location)
                {
                    grid.HighlightUnitCell(selectedUnit.Location.Index);
                }
            }
            
            // Если навык требует прямой видимости, вычисляем траекторию
            if (selectedSkill.requiresLineOfSight)
            {
                DoFindTrajectory(currentCell);
            }
            
            // Проверяем, является ли клетка валидной целью для навыка (только при смене ячейки)
            bool isValidTarget = selectedSkill.IsValidTarget(currentCell, selectedUnit);
            
            // Показываем предпросмотр траектории только при смене ячейки
            if (selectedSkill.requiresLineOfSight)
            {
                selectedSkill.ShowTargetingPreview(currentCell, selectedUnit);
            }
            
            // Логируем результат проверки цели
            if (isValidTarget)
            {
                // Подсвечиваем валидную цель
                grid.HighlightCell(currentCell.Index, Color.red);
            }
            else
            {
                // Подсвечиваем невалидную цель серым
                grid.HighlightCell(currentCell.Index, Color.gray);
            }
            
            previousActionTargetCell = currentCell;
        }
    }
    
    void DoFindTrajectory(HexCell targetCell)
    {
        if (selectedUnit == null || targetCell == null)
        {
            ClearTrajectory();
            previousTrajectoryCell = null;
            return;
        }
        
        // Пересчитываем траекторию только если ячейка изменилась
        if (previousTrajectoryCell != targetCell)
        {
            BattleHexGrid battleGrid = grid as BattleHexGrid;
            if (battleGrid != null)
            {
                battleGrid.FindTrajectory(selectedUnit.Location, targetCell);
            }
            previousTrajectoryCell = targetCell;
        }
    }
    
    void ClearTrajectory()
    {
        BattleHexGrid battleGrid = grid as BattleHexGrid;
        if (battleGrid != null)
        {
            battleGrid.ClearTrajectory();
        }
        previousTrajectoryCell = null;
    }
    
    void ClearActionTargeting()
    {
        if (previousActionTargetCell.HasValue)
        {
            grid.DisableHighlight(previousActionTargetCell.Value.Index);
            previousActionTargetCell = null;
        }
        
        if (currentCell != null)
        {
            grid.DisableHighlight(currentCell.Index);
        }
        
        if (selectedSkill != null)
        {
            selectedSkill.HideTargetingPreview();
        }
        
        // Очищаем траекторию
        ClearTrajectory();
        
        // Подсвечиваем позицию юнита, если он есть и управляется человеком
        if (selectedUnit != null && IsHumanControlledUnitSelected())
        {
            grid.HighlightUnitCell(selectedUnit.Location.Index);
        }
    }
}