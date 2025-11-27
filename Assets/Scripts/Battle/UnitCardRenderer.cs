using UnityEngine;

/// <summary>
/// Компонент для отображения юнита в виде карточки (как в Wildermyth)
/// Использует префаб UnitCardBase для отображения карточки с анимациями и эффектами
/// </summary>
public class UnitCardRenderer : MonoBehaviour
{
    [Header("Card Prefab")]
    [SerializeField] private GameObject unitCardPrefab; // Префаб UnitCardBase (если не задан, загружается из Resources)
    
    [Header("Card Display")]
    private SpriteRenderer spriteRenderer;
    private Transform cardTransform; // Transform дочернего объекта карточки
    private UnitCardAnimatorController animatorController;
    private UnitCardStatusEffects statusEffects;
    private UnitCardHighlight cardHighlight; // Компонент подсветки
    private UnitHealthBar healthBar; // Плашка здоровья
    
    [Header("Card Settings")]
    [SerializeField] private float cardElevation = 0f; // Высота карточки над поверхностью (0 = нижний край на уровне гекса)
    
    // Фиксированный размер карточки для всех юнитов
    private const float FIXED_CARD_SIZE = 2f; // Все карточки 2x2
    [SerializeField] private bool alwaysFaceCamera = true; // Всегда поворачивать карточку к камере
    [SerializeField] private float cardScaleMultiplier = 5f; // Множитель масштаба карточки (для увеличения размера)
    [SerializeField] private float maxTiltAngle = 60f; // Максимальный угол наклона карточки к камере при зуме (в градусах)
    [SerializeField] private bool maintainAspectRatio = true; // Сохранять пропорции спрайта при масштабировании (рекомендуется включить)
    
    private Camera mainCamera;
    private HexMapCamera hexMapCamera;
    private UnitData unitData;
    private const string CARD_CHILD_NAME = "UnitCard";
    private const string CARD_SPRITE_NAME = "CardSprite";
    private const string CARD_PIVOT_NAME = "CardPivot";
    
    void Awake()
    {
        // Ищем существующую карточку среди дочерних объектов
        // Сначала проверяем прямых детей по имени
        Transform existingCard = transform.Find(CARD_CHILD_NAME);
        
        // Если не найдено, ищем по альтернативному имени или по компоненту
        if (existingCard == null)
        {
            existingCard = transform.Find("UnitCardBase");
        }
        
        if (existingCard == null)
        {
            // Ищем по компоненту Animator (префаб должен иметь его)
            Animator existingAnimator = GetComponentInChildren<Animator>();
            if (existingAnimator != null && existingAnimator.GetComponent<UnitCardAnimatorController>() != null)
            {
                existingCard = existingAnimator.transform;
                // Поднимаемся до корня префаба (пока не достигнем transform)
                while (existingCard.parent != null && existingCard.parent != transform)
                {
                    existingCard = existingCard.parent;
                }
            }
        }
        
        if (existingCard != null)
        {
            cardTransform = existingCard;
            // Убеждаемся, что имя правильное
            if (cardTransform.name != CARD_CHILD_NAME)
            {
                cardTransform.name = CARD_CHILD_NAME;
            }
        }
        else
        {
            // Загружаем префаб, если не задан в инспекторе
            if (unitCardPrefab == null)
            {
                // Пытаемся загрузить из Resources
                unitCardPrefab = Resources.Load<GameObject>("UnitCardBase");
                if (unitCardPrefab == null)
                {
                    // Если не найден в Resources, пытаемся найти через AssetDatabase (только в Editor)
                    #if UNITY_EDITOR
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("UnitCardBase t:Prefab");
                    if (guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        unitCardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    }
                    #endif
                }
            }
            
            if (unitCardPrefab != null)
            {
                // Создаем экземпляр префаба
                GameObject cardInstance = Instantiate(unitCardPrefab, transform);
                cardInstance.name = CARD_CHILD_NAME;
                cardTransform = cardInstance.transform;
                cardTransform.localPosition = Vector3.zero;
                cardTransform.localRotation = Quaternion.identity;
                cardTransform.localScale = Vector3.one;
            }
            else
            {
                // Fallback: создаем простой GameObject, если префаб не найден
                Debug.LogWarning("UnitCardBase prefab not found! Creating simple card GameObject.");
                GameObject cardObject = new GameObject(CARD_CHILD_NAME);
                cardTransform = cardObject.transform;
                cardTransform.SetParent(transform);
                cardTransform.localPosition = Vector3.zero;
                cardTransform.localRotation = Quaternion.identity;
                cardTransform.localScale = Vector3.one;
            }
        }
        
        // Инициализируем компоненты карточки
        InitializeCardComponents();
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        // Ищем HexMapCamera для отслеживания угла наклона при зуме
        hexMapCamera = FindFirstObjectByType<HexMapCamera>();
    }
    
    /// <summary>
    /// Инициализирует компоненты карточки (SpriteRenderer, Animator, StatusEffects)
    /// Также корректирует позиции CardPivot и CardSprite для правильной анимации смерти
    /// </summary>
    private void InitializeCardComponents()
    {
        if (cardTransform == null)
            return;
        
        // Ищем CardPivot и CardSprite
        // Структура префаба: UnitCardBase -> CardPivot -> CardSprite
        Transform cardPivot = cardTransform.Find(CARD_PIVOT_NAME);
        Transform cardSpriteTransform = null;
        
        if (cardPivot != null)
        {
            cardSpriteTransform = cardPivot.Find(CARD_SPRITE_NAME);
        }
        
        // Если CardPivot не найден, ищем CardSprite рекурсивно
        if (cardSpriteTransform == null)
        {
            cardSpriteTransform = FindInChildren(cardTransform, CARD_SPRITE_NAME);
        }
        
        if (cardSpriteTransform != null)
        {
            spriteRenderer = cardSpriteTransform.GetComponent<SpriteRenderer>();
        }
        else
        {
            // Fallback: ищем SpriteRenderer на корневом объекте карточки
            spriteRenderer = cardTransform.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                // Создаем SpriteRenderer, если не найден
                spriteRenderer = cardTransform.gameObject.AddComponent<SpriteRenderer>();
            }
        }
        
        // Получаем Animator Controller (префаб уже должен иметь его на корневом объекте)
        animatorController = cardTransform.GetComponent<UnitCardAnimatorController>();
        if (animatorController == null)
        {
            // Если компонент не найден, добавляем его
            animatorController = cardTransform.gameObject.AddComponent<UnitCardAnimatorController>();
        }
        
        // Получаем Status Effects (префаб уже должен иметь его на корневом объекте)
        statusEffects = cardTransform.GetComponent<UnitCardStatusEffects>();
        if (statusEffects == null)
        {
            // Если компонент не найден, добавляем его
            statusEffects = cardTransform.gameObject.AddComponent<UnitCardStatusEffects>();
        }
        
        // Получаем или создаем компонент подсветки на объекте со SpriteRenderer
        GameObject highlightTarget = (cardSpriteTransform != null) ? cardSpriteTransform.gameObject : cardTransform.gameObject;
        cardHighlight = highlightTarget.GetComponent<UnitCardHighlight>();
        if (cardHighlight == null)
        {
            // Если компонент не найден, добавляем его
            cardHighlight = highlightTarget.AddComponent<UnitCardHighlight>();
        }
    }
    
    /// <summary>
    /// Рекурсивно ищет дочерний объект по имени
    /// </summary>
    private Transform FindInChildren(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrEmpty(name))
            return null;
            
        // Сначала проверяем прямых детей
        Transform found = parent.Find(name);
        if (found != null)
            return found;
            
        // Затем рекурсивно ищем в дочерних объектах
        foreach (Transform child in parent)
        {
            found = FindInChildren(child, name);
            if (found != null)
                return found;
        }
        
        return null;
    }
    
    void Start()
    {
        // Позиция карточки устанавливается после размещения юнита на гексе
        // через UpdatePositionWithHexElevation()
    }
    
    void LateUpdate()
    {
        // Постоянно обновляем позицию карточки с учетом высоты текущего гекса
        // Это важно при перемещении юнита по рельефу
        UpdatePositionWithHexElevation();
        
        // Обновляем поворот карточки к камере
        if (alwaysFaceCamera && mainCamera != null)
        {
            UpdateCardRotation();
        }
    }
    
    /// <summary>
    /// Обновляет поворот карточки с учетом угла наклона камеры при зуме
    /// Карточка всегда развернута к камере, но с ограничениями по углу поворота и наклона
    /// </summary>
    private void UpdateCardRotation()
    {
        if (cardTransform == null || mainCamera == null)
            return;
            
        // Направление от карточки к камере (в мировых координатах)
        Vector3 directionToCamera = mainCamera.transform.position - cardTransform.position;
        
        // Если камера очень близко, ничего не делаем
        if (directionToCamera.sqrMagnitude < 0.01f)
            return;
        
        // Горизонтальное направление к камере (для базового поворота)
        Vector3 horizontalDirection = directionToCamera;
        horizontalDirection.y = 0;
        
        // Если камера точно сверху, используем направление по умолчанию
        if (horizontalDirection.sqrMagnitude < 0.01f)
        {
            horizontalDirection = transform.forward;
            horizontalDirection.y = 0;
            if (horizontalDirection.sqrMagnitude < 0.01f)
                horizontalDirection = Vector3.forward;
        }
        
        horizontalDirection.Normalize();
        
        // Карточка всегда смотрит на камеру без ограничений
        // Вычисляем финальный горизонтальный поворот к камере
        Quaternion finalHorizontalRotation = Quaternion.LookRotation(-horizontalDirection);
        
        // Получаем угол наклона камеры (от 0 до 90 градусов, где 90 - вид сверху)
        float cameraTiltAngle = GetCameraTiltAngle();
        
        // Вычисляем угол наклона карточки (0-maxTiltAngle градусов в зависимости от угла камеры)
        // Когда камера смотрит сверху (90°), карточка наклоняется на maxTiltAngle
        // Когда камера смотрит сбоку (45°), карточка не наклоняется (0°)
        float tiltAngle = 0f;
        if (cameraTiltAngle > 45f)
        {
            // Интерполируем от 45° (0 наклона) до 90° (maxTiltAngle наклона)
            float t = Mathf.InverseLerp(45f, 90f, cameraTiltAngle);
            tiltAngle = Mathf.Lerp(0f, maxTiltAngle, t);
        }
        
        // Применяем наклон по оси X (поворот вверх к камере)
        Quaternion tiltRotation = Quaternion.Euler(tiltAngle, 0f, 0f);
        cardTransform.rotation = finalHorizontalRotation * tiltRotation;
    }
    
    /// <summary>
    /// Получает угол наклона камеры относительно горизонтали (45-90 градусов)
    /// 45 = вид сбоку, 90 = вид сверху
    /// </summary>
    private float GetCameraTiltAngle()
    {
        if (hexMapCamera != null)
        {
            try
            {
                // Получаем swivel из HexMapCamera (первый дочерний объект)
                Transform swivel = hexMapCamera.transform.GetChild(0);
                if (swivel != null)
                {
                    // Получаем угол наклона swivel (от swivelMinZoom до swivelMaxZoom)
                    float swivelAngle = swivel.localEulerAngles.x;
                    
                    // Нормализуем угол к диапазону 0-360
                    if (swivelAngle > 180f)
                        swivelAngle -= 360f;
                    
                    // Возвращаем абсолютное значение (обычно от 45 до 90 градусов)
                    return Mathf.Abs(swivelAngle);
                }
            }
            catch (System.Exception ex)
            {
                // Если не удалось получить swivel, используем fallback
                Debug.LogWarning($"Не удалось получить swivel из HexMapCamera: {ex.Message}");
            }
        }
        
        // Fallback: вычисляем угол из направления камеры
        if (mainCamera != null)
        {
            Vector3 cameraForward = mainCamera.transform.forward;
            // Вычисляем угол между направлением камеры и горизонтальной плоскостью
            // angle = 0 когда камера смотрит горизонтально, 90 когда смотрит вниз
            float angle = Vector3.Angle(Vector3.down, cameraForward);
            // Инвертируем: 90 - угол, чтобы получить наклон камеры (0 = горизонтально, 90 = сверху)
            return 90f - angle;
        }
        
        return 45f; // Значение по умолчанию (вид сбоку)
    }
    
    /// <summary>
    /// Инициализирует карточку из UnitData
    /// </summary>
    /// <param name="data">Данные юнита</param>
    /// <param name="flipX">Зеркалировать карточку по оси X (для игрока 2)</param>
    /// <param name="isTeam1">Является ли юнит командой 1 (true) или командой 2 (false)</param>
    public void InitializeFromUnitData(UnitData data, bool flipX = false, bool isTeam1 = true)
    {
        if (data == null)
        {
            Debug.LogError("UnitData is null in UnitCardRenderer");
            return;
        }
        
        unitData = data;
        
        // Устанавливаем спрайт карточки
        if (data.unitCardSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = data.unitCardSprite;
            
            // Устанавливаем зеркалирование по оси X
            spriteRenderer.flipX = flipX;
            
            // Устанавливаем размер карточки (принудительно 2x2 для всех юнитов)
            Vector2 fixedCardSize = new Vector2(2f, 2f); // Фиксированный размер 2x2 для всех юнитов
            if (fixedCardSize != Vector2.zero)
            {
                // Вычисляем масштаб на основе размера карточки и размера спрайта
                float spriteWidth = data.unitCardSprite.bounds.size.x;
                float spriteHeight = data.unitCardSprite.bounds.size.y;
                
                if (spriteWidth > 0 && spriteHeight > 0)
                {
                    float scaleX = (fixedCardSize.x / spriteWidth) * cardScaleMultiplier;
                    float scaleY = (fixedCardSize.y / spriteHeight) * cardScaleMultiplier;
                    
                    // Если нужно сохранять пропорции спрайта, используем единый масштаб
                    if (maintainAspectRatio)
                    {
                        // Используем среднее значение масштабов, чтобы сохранить пропорции
                        // и примерно соответствовать желаемому размеру карточки
                        float uniformScale = (scaleX + scaleY) * 0.5f;
                        cardTransform.localScale = new Vector3(uniformScale, uniformScale, 1f);
                    }
                    else
                    {
                        // Используем независимые масштабы для X и Y
                        cardTransform.localScale = new Vector3(scaleX, scaleY, 1f);
                    }
                }
            }
            else
            {
                // Если размер не задан, используем только множитель
                cardTransform.localScale = new Vector3(cardScaleMultiplier, cardScaleMultiplier, 1f);
            }
            
            // Настраиваем порядок отрисовки (чтобы карточки отображались поверх других объектов)
            spriteRenderer.sortingOrder = 100;
        }
        else
        {
            if (spriteRenderer == null)
            {
                Debug.LogError($"SpriteRenderer не найден для карточки юнита {data.unitName}!");
            }
            else
            {
                Debug.LogWarning($"UnitCardSprite не установлен для юнита {data.unitName}. Карточка не будет отображаться.");
            }
        }
        
        // Устанавливаем начальное статичное состояние (без анимаций, т.к. юнит еще неактивен)
        SetStaticState();
        
        // Настраиваем подсветку в зависимости от команды
        if (cardHighlight != null)
        {
            cardHighlight.SetTeam(isTeam1);
            cardHighlight.SetActive(false); // По умолчанию неактивен
        }
        
        // Инициализируем плашку здоровья
        InitializeHealthBar(data);
        
        // Позиция карточки будет установлена после размещения юнита на гексе
        // через UpdatePositionWithHexElevation()
    }
    
    /// <summary>
    /// Инициализирует плашку здоровья
    /// </summary>
    private void InitializeHealthBar(UnitData data)
    {
        if (cardTransform == null)
            return;
        
        // Ищем HealthBarCanvas в дочерних объектах карточки
        Transform healthBarCanvas = FindInChildren(cardTransform, "HealthBarCanvas");
        if (healthBarCanvas == null)
        {
            Debug.LogWarning("UnitCardRenderer: HealthBarCanvas не найден в префабе карточки!");
            return;
        }
        
        // Получаем или создаем компонент UnitHealthBar
        healthBar = healthBarCanvas.GetComponent<UnitHealthBar>();
        if (healthBar == null)
        {
            healthBar = healthBarCanvas.gameObject.AddComponent<UnitHealthBar>();
        }
        
        // Настраиваем Canvas для отображения плашки здоровья поверх других объектов
        Canvas canvas = healthBarCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            // Включаем переопределение сортировки и устанавливаем высокий sorting order
            // чтобы плашка здоровья отображалась поверх карточек юнитов (sortingOrder=100) и выделения поля
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200; // Выше, чем у карточек юнитов (100)
        }
        
        // Инициализируем плашку здоровья
        if (healthBar != null && data != null)
        {
            healthBar.Initialize(data.unitName, data.maxHealth, data.maxHealth);
        }
    }
    
    /// <summary>
    /// Обновляет плашку здоровья
    /// </summary>
    /// <param name="currentHealth">Текущее здоровье</param>
    /// <param name="maxHealth">Максимальное здоровье</param>
    /// <param name="animate">Анимировать ли изменение</param>
    public void UpdateHealthBar(int currentHealth, int maxHealth, bool animate = true)
    {
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, animate);
        }
    }
    
    
    /// <summary>
    /// Обновляет позицию карточки с учетом высоты гекса
    /// Вызывается после того, как юнит размещен на гексе и во время перемещения
    /// </summary>
    public void UpdatePositionWithHexElevation()
    {
        if (cardTransform == null)
            return;
            
        // UnitCardRenderer находится на том же GameObject, что и BattleHexUnit
        // cardTransform - это дочерний объект UnitCard (корневой объект префаба UnitCardBase)
        // 
        // Структура префаба UnitCardBase:
        // - UnitCardBase (cardTransform) - корневой объект
        // - CardPivot - localPosition.y = 0.0 (точка вращения внизу карточки для анимации смерти)
        // - CardSprite - localPosition.y = 1.0 относительно CardPivot (центр спрайта на высоте 1.0)
        //
        // Логика позиционирования:
        // - CardPivot на Y=0.0 - точка вращения внизу карточки
        // - CardSprite на Y=1.0 относительно CardPivot - центр спрайта на высоте 1.0
        // - Для карточки 2x2: центр спрайта на Y=1.0, нижний край на Y=0.0 относительно UnitCardBase
        // - Чтобы нижний край был на уровне гекса (Y=0 относительно родителя), UnitCardBase должен быть на Y=0
        // - cardElevation позволяет добавить небольшое смещение вверх, если нужно (по умолчанию 0)
        
        Vector3 cardLocalPosition = cardTransform.localPosition;
        cardLocalPosition.x = 0f;
        cardLocalPosition.y = cardElevation; // cardElevation = 0 по умолчанию (нижний край на уровне гекса)
        cardLocalPosition.z = 0f;
        cardTransform.localPosition = cardLocalPosition;
    }
    
    /// <summary>
    /// Устанавливает видимость карточки
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (cardTransform != null)
        {
            cardTransform.gameObject.SetActive(visible);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }
    }
    
    /// <summary>
    /// Устанавливает цвет карточки (для эффектов, например, затемнение при смерти)
    /// </summary>
    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
    
    /// <summary>
    /// Сбрасывает цвет карточки к белому
    /// </summary>
    public void ResetColor()
    {
        SetColor(Color.white);
    }
    
    // ========== Методы управления анимациями ==========
    
    /// <summary>
    /// Устанавливает состояние анимации карточки
    /// </summary>
    public void SetAnimationState(UnitCardAnimatorController.CardAnimationState state)
    {
        if (animatorController != null)
        {
            animatorController.SetAnimationState(state);
        }
    }
    
    /// <summary>
    /// Проигрывает анимацию атаки
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (animatorController != null)
        {
            animatorController.PlayAttackAnimation();
        }
    }
    
    /// <summary>
    /// Проигрывает анимацию получения урона
    /// </summary>
    /// <param name="wasActive">Был ли юнит активен до урона (чтобы вернуть правильное состояние после анимации)</param>
    public void PlayHurtAnimation(bool wasActive = true)
    {
        if (animatorController == null)
        {
            Debug.LogWarning("UnitCardRenderer: animatorController is null, cannot play hurt animation");
            return;
        }
        
        // Временно включаем аниматор, если он был выключен (для неактивных юнитов)
        bool animatorWasEnabled = animatorController.IsAnimatorEnabled();
        
        if (!animatorWasEnabled)
        {
            Debug.Log($"Включаю аниматор для проигрывания анимации Hurt на юните {gameObject.name}");
            animatorController.EnableAnimator();
        }
        
        // Убеждаемся, что аниматор включен перед проигрыванием
        if (animatorController.IsAnimatorEnabled())
        {
            animatorController.PlayHurtAnimation();
            Debug.Log($"Проигрываю анимацию Hurt на юните {gameObject.name} (wasActive: {wasActive})");
        }
        else
        {
            Debug.LogError($"Не удалось включить аниматор для юнита {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Проигрывает анимацию смерти
    /// </summary>
    public void PlayDeadAnimation()
    {
        if (animatorController == null)
        {
            Debug.LogWarning("UnitCardRenderer: animatorController is null, cannot play dead animation");
            return;
        }
        
        // Включаем аниматор, если он был выключен
        if (!animatorController.IsAnimatorEnabled())
        {
            Debug.Log($"Включаю аниматор для проигрывания анимации Dead на юните {gameObject.name}");
            animatorController.EnableAnimator();
        }
        
        // Убеждаемся, что аниматор включен перед проигрыванием
        if (animatorController.IsAnimatorEnabled())
        {
            animatorController.PlayDeadAnimation();
            Debug.Log($"Проигрываю анимацию Dead на юните {gameObject.name}");
        }
        else
        {
            Debug.LogError($"Не удалось включить аниматор для юнита {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Устанавливает анимацию покоя
    /// </summary>
    public void SetIdleAnimation()
    {
        if (animatorController != null)
        {
            animatorController.SetIdleAnimation();
        }
    }
    
    /// <summary>
    /// Устанавливает анимацию движения
    /// </summary>
    public void SetMoveAnimation(bool isMoving)
    {
        if (animatorController != null)
        {
            animatorController.SetMoveAnimation(isMoving);
        }
    }
    
    // ========== Методы управления эффектами состояний ==========
    
    /// <summary>
    /// Устанавливает эффект статуса (включает или выключает)
    /// </summary>
    public void SetStatusEffect(StatusType status, bool active)
    {
        if (statusEffects != null)
        {
            statusEffects.SetStatusEffect(status, active);
        }
    }
    
    /// <summary>
    /// Очищает все активные эффекты статусов
    /// </summary>
    public void ClearAllStatusEffects()
    {
        if (statusEffects != null)
        {
            statusEffects.ClearAllStatusEffects();
        }
    }
    
    // ========== Методы управления подсветкой ==========
    
    /// <summary>
    /// Устанавливает активность юнита (для подсветки)
    /// </summary>
    public void SetUnitActive(bool active)
    {
        if (cardHighlight != null)
        {
            cardHighlight.SetActive(active);
        }
    }
    
    /// <summary>
    /// Устанавливает команду юнита (для подсветки)
    /// </summary>
    public void SetUnitTeam(bool isTeam1)
    {
        if (cardHighlight != null)
        {
            cardHighlight.SetTeam(isTeam1);
        }
    }
    
    /// <summary>
    /// Обновляет подсветку на основе команды и активности
    /// </summary>
    public void UpdateHighlight(bool isTeam1, bool isActive)
    {
        if (cardHighlight != null)
        {
            cardHighlight.SetTeam(isTeam1);
            cardHighlight.SetActive(isActive);
        }
    }
    
    /// <summary>
    /// Отключает подсветку
    /// </summary>
    public void DisableHighlight()
    {
        if (cardHighlight != null)
        {
            cardHighlight.DisableHighlight();
        }
    }
    
    /// <summary>
    /// Включает подсветку
    /// </summary>
    public void EnableHighlight()
    {
        if (cardHighlight != null)
        {
            cardHighlight.EnableHighlight();
        }
    }
    
    /// <summary>
    /// Устанавливает статичное состояние (без анимаций)
    /// Используется когда юнит неактивен
    /// </summary>
    public void SetStaticState()
    {
        if (animatorController != null)
        {
            animatorController.SetStaticState();
        }
    }
    
    /// <summary>
    /// Включает аниматор обратно
    /// Используется когда юнит становится активным
    /// </summary>
    public void EnableAnimator()
    {
        if (animatorController != null)
        {
            animatorController.EnableAnimator();
        }
    }
    
}


