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
    
    [Header("Card Settings")]
    [SerializeField] private float cardElevation = 6f; // Высота карточки над поверхностью
    [SerializeField] private bool alwaysFaceCamera = true; // Всегда поворачивать карточку к камере
    [SerializeField] private float cardScaleMultiplier = 5f; // Множитель масштаба карточки (для увеличения размера)
    [SerializeField] private float maxTiltAngle = 60f; // Максимальный угол наклона карточки к камере при зуме (в градусах)
    [SerializeField] private bool maintainAspectRatio = true; // Сохранять пропорции спрайта при масштабировании (рекомендуется включить)
    
    private Camera mainCamera;
    private HexMapCamera hexMapCamera;
    private UnitData unitData;
    private const string CARD_CHILD_NAME = "UnitCard";
    private const string CARD_SPRITE_NAME = "CardSprite";
    
    void Awake()
    {
        // Ищем или создаем карточку из префаба
        Transform existingCard = transform.Find(CARD_CHILD_NAME);
        if (existingCard != null)
        {
            cardTransform = existingCard;
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
    /// </summary>
    private void InitializeCardComponents()
    {
        if (cardTransform == null)
            return;
        
        // Ищем CardSprite (основной спрайт карточки)
        Transform cardSpriteTransform = cardTransform.Find(CARD_SPRITE_NAME);
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
        
        // Получаем Animator Controller
        animatorController = cardTransform.GetComponent<UnitCardAnimatorController>();
        if (animatorController == null)
        {
            // Если компонент не найден, добавляем его
            animatorController = cardTransform.gameObject.AddComponent<UnitCardAnimatorController>();
        }
        
        // Получаем Status Effects
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
            catch
            {
                // Если не удалось получить swivel, используем fallback
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
            
            // Устанавливаем размер карточки
            if (data.cardSize != Vector2.zero)
            {
                // Вычисляем масштаб на основе размера карточки и размера спрайта
                float spriteWidth = data.unitCardSprite.bounds.size.x;
                float spriteHeight = data.unitCardSprite.bounds.size.y;
                
                if (spriteWidth > 0 && spriteHeight > 0)
                {
                    float scaleX = (data.cardSize.x / spriteWidth) * cardScaleMultiplier;
                    float scaleY = (data.cardSize.y / spriteHeight) * cardScaleMultiplier;
                    
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
        
        // Позиция карточки будет установлена после размещения юнита на гексе
        // через UpdatePositionWithHexElevation()
    }
    
    private float lastBaseY = float.MinValue; // Кешируем последнюю Y позицию базового объекта
    
    /// <summary>
    /// Обновляет позицию карточки с учетом высоты гекса
    /// Вызывается после того, как юнит размещен на гексе и во время перемещения
    /// </summary>
    public void UpdatePositionWithHexElevation()
    {
        if (cardTransform == null)
            return;
            
        // UnitCardRenderer находится на том же GameObject, что и BattleHexUnit
        // cardTransform - это дочерний объект для карточки
        // Во время перемещения позиция родителя (юнита) обновляется через transform.localPosition в TravelPath()
        // которая уже включает высоту гекса через Bezier.GetPoint
        // Нам нужно установить локальную позицию дочернего объекта (карточки) так, чтобы Y = cardElevation
        
        // Базовая позиция родителя (юнита) уже содержит правильную высоту гекса:
        // - При размещении: устанавливается в Location.Position (который включает высоту)
        // - При перемещении: интерполируется через Bezier.GetPoint (который учитывает высоты гексов)
        
        // Локальная позиция карточки относительно родителя (юнита)
        // X и Z должны быть 0 (карточка над юнитом), Y = cardElevation (высота над юнитом)
        Vector3 parentPosition = transform.localPosition;
        float targetY = cardElevation;
        
        // Кешируем Y позиции родителя для оптимизации
        bool parentMoved = Mathf.Abs(lastBaseY - parentPosition.y) > 0.001f;
        lastBaseY = parentPosition.y;
        
        // Обновляем локальную позицию карточки относительно родителя
        Vector3 cardLocalPosition = cardTransform.localPosition;
        if (parentMoved || Mathf.Abs(cardLocalPosition.y - targetY) > 0.01f)
        {
            // Устанавливаем позицию карточки: над родителем на высоте cardElevation
            cardLocalPosition.x = 0f;
            cardLocalPosition.y = targetY;
            cardLocalPosition.z = 0f;
            cardTransform.localPosition = cardLocalPosition;
        }
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


