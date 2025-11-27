using UnityEngine;
using System.Collections;

/// <summary>
/// Компонент для управления анимацией получения урона карточкой через шейдер
/// Деформирует карточку в параллелограмм (сдвиг верхней грани влево) и возвращает обратно
/// Аналогично CardAttackAnimation, но сдвиг происходит влево вместо вправо
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CardHurtAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Максимальный сдвиг верхней грани (в единицах спрайта)")]
    [SerializeField] public float maxSkewAmount = 0.3f;
    
    [Tooltip("Длительность движения влево (секунды)")]
    [SerializeField] public float forwardDuration = 0.15f;
    
    [Tooltip("Длительность застывания в наклоненном положении (секунды)")]
    [SerializeField] public float holdDuration = 0.1f;
    
    [Tooltip("Длительность возврата в исходное положение (секунды)")]
    [SerializeField] public float returnDuration = 0.2f;
    
    [Tooltip("Кривая анимации для движения вперед")]
    [SerializeField] private AnimationCurve forwardCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("Кривая анимации для возврата")]
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    
    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Material originalMaterial;
    private Material hurtMaterial;
    private bool isAnimating = false;
    
    // Имя шейдера (используем универсальный шейдер)
    private const string SHADER_NAME = "Custom/CardUniversal";
    private const string PROPERTY_SKEW_AMOUNT = "_HurtSkewAmount";
    
    void Awake()
    {
        InitializeComponents();
    }
    
    void OnEnable()
    {
        // Убеждаемся, что компоненты инициализированы
        if (spriteRenderer == null)
        {
            InitializeComponents();
        }
    }
    
    /// <summary>
    /// Инициализирует компоненты и создает материал анимации
    /// </summary>
    private void InitializeComponents()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("CardHurtAnimation: SpriteRenderer не найден!");
                return;
            }
        }
        
        // Сохраняем оригинальный материал
        if (originalMaterial == null)
        {
            originalMaterial = spriteRenderer.sharedMaterial;
        }
        
        // Создаем MaterialPropertyBlock для изменения параметров без создания нового материала
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
        
        // Создаем материал анимации, если его еще нет
        EnsureHurtMaterial();
    }
    
    /// <summary>
    /// Убеждается, что материал анимации создан
    /// Теперь использует универсальный шейдер, который поддерживает подсветку
    /// </summary>
    private void EnsureHurtMaterial()
    {
        // Проверяем, есть ли уже материал с универсальным шейдером (от UnitCardHighlight)
        Material currentMaterial = spriteRenderer != null ? spriteRenderer.sharedMaterial : null;
        
        // Если текущий материал уже использует универсальный шейдер, используем его
        if (currentMaterial != null && currentMaterial.shader != null && 
            currentMaterial.shader.name == "Custom/CardUniversal")
        {
            // Не создаем новый материал, будем использовать MaterialPropertyBlock
            return;
        }
        
        // Если материала нет или он не универсальный, создаем новый
        if (hurtMaterial != null)
            return;
        
        // Загружаем универсальный шейдер
        Shader universalShader = Shader.Find(SHADER_NAME);
        if (universalShader == null)
        {
            Debug.LogWarning($"CardHurtAnimation: Шейдер {SHADER_NAME} не найден! Анимация получения урона не будет работать.");
            return;
        }
        
        hurtMaterial = new Material(universalShader);
        
        // Копируем текстуру и цвет из оригинального материала
        if (originalMaterial != null)
        {
            if (originalMaterial.HasProperty("_MainTex"))
            {
                hurtMaterial.SetTexture("_MainTex", originalMaterial.GetTexture("_MainTex"));
            }
            if (originalMaterial.HasProperty("_Color"))
            {
                hurtMaterial.SetColor("_Color", originalMaterial.GetColor("_Color"));
            }
            // Копируем параметры подсветки, если есть
            if (originalMaterial.HasProperty("_OutlineColor"))
            {
                hurtMaterial.SetColor("_OutlineColor", originalMaterial.GetColor("_OutlineColor"));
            }
            if (originalMaterial.HasProperty("_OutlineWidth"))
            {
                hurtMaterial.SetFloat("_OutlineWidth", originalMaterial.GetFloat("_OutlineWidth"));
            }
            if (originalMaterial.HasProperty("_OutlineGlow"))
            {
                hurtMaterial.SetFloat("_OutlineGlow", originalMaterial.GetFloat("_OutlineGlow"));
            }
        }
        // Если оригинального материала нет, используем текстуру из SpriteRenderer
        else if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            hurtMaterial.SetTexture("_MainTex", spriteRenderer.sprite.texture);
        }
    }
    
    /// <summary>
    /// Запускает анимацию получения урона
    /// </summary>
    public void PlayHurtAnimation()
    {
        if (isAnimating)
        {
            Debug.LogWarning("CardHurtAnimation: Анимация уже проигрывается!");
            return;
        }
        
        // Убеждаемся, что компоненты инициализированы
        if (spriteRenderer == null)
        {
            InitializeComponents();
        }
        
        // Убеждаемся, что материал создан
        EnsureHurtMaterial();
        
        // Проверяем, что либо есть материал анимации, либо текущий материал поддерживает универсальный шейдер
        Material currentMaterial = spriteRenderer.sharedMaterial;
        bool hasUniversalShader = (currentMaterial != null && currentMaterial.shader != null && 
                                  currentMaterial.shader.name == "Custom/CardUniversal");
        
        if (hurtMaterial == null && !hasUniversalShader)
        {
            Debug.LogWarning($"CardHurtAnimation: Материал анимации не создан! Проверьте, что шейдер {SHADER_NAME} существует.");
            return;
        }
        
        StartCoroutine(HurtAnimationCoroutine());
    }
    
    /// <summary>
    /// Корутина анимации получения урона
    /// </summary>
    private IEnumerator HurtAnimationCoroutine()
    {
        isAnimating = true;
        
        // Проверяем, использует ли текущий материал универсальный шейдер
        Material currentMaterial = spriteRenderer.sharedMaterial;
        bool useCurrentMaterial = (currentMaterial != null && currentMaterial.shader != null && 
                                   currentMaterial.shader.name == "Custom/CardUniversal");
        
        // Если текущий материал уже универсальный, используем его (сохраняем подсветку)
        // Иначе переключаемся на материал анимации
        if (!useCurrentMaterial && hurtMaterial != null)
        {
            spriteRenderer.material = hurtMaterial;
        }
        
        // Сохраняем флаг для использования в конце корутины
        bool shouldRestoreMaterial = !useCurrentMaterial;
        
        // Фаза 1: Быстрое движение влево (деформация в параллелограмм)
        float elapsed = 0f;
        while (elapsed < forwardDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / forwardDuration;
            float curveValue = forwardCurve.Evaluate(t);
            float skewAmount = curveValue * maxSkewAmount;
            
            propertyBlock.SetFloat(PROPERTY_SKEW_AMOUNT, skewAmount);
            spriteRenderer.SetPropertyBlock(propertyBlock);
            
            yield return null;
        }
        
        // Устанавливаем максимальный сдвиг
        propertyBlock.SetFloat(PROPERTY_SKEW_AMOUNT, maxSkewAmount);
        spriteRenderer.SetPropertyBlock(propertyBlock);
        
        // Фаза 2: Застывание в наклоненном положении
        yield return new WaitForSeconds(holdDuration);
        
        // Фаза 3: Возврат в исходное положение
        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            float curveValue = returnCurve.Evaluate(t);
            float skewAmount = curveValue * maxSkewAmount;
            
            propertyBlock.SetFloat(PROPERTY_SKEW_AMOUNT, skewAmount);
            spriteRenderer.SetPropertyBlock(propertyBlock);
            
            yield return null;
        }
        
        // Сбрасываем сдвиг
        propertyBlock.SetFloat(PROPERTY_SKEW_AMOUNT, 0f);
        spriteRenderer.SetPropertyBlock(propertyBlock);
        
        // Возвращаемся к оригинальному материалу только если мы его меняли
        if (shouldRestoreMaterial && originalMaterial != null)
        {
            spriteRenderer.material = originalMaterial;
        }
        
        isAnimating = false;
    }
    
    /// <summary>
    /// Останавливает анимацию и возвращает карточку в исходное состояние
    /// </summary>
    public void StopAnimation()
    {
        if (isAnimating)
        {
            StopAllCoroutines();
            isAnimating = false;
        }
        
        if (propertyBlock != null)
        {
            propertyBlock.SetFloat(PROPERTY_SKEW_AMOUNT, 0f);
            spriteRenderer.SetPropertyBlock(propertyBlock);
        }
        
        if (spriteRenderer != null && originalMaterial != null)
        {
            spriteRenderer.material = originalMaterial;
        }
    }
    
    void OnDestroy()
    {
        // Очищаем созданный материал
        if (hurtMaterial != null)
        {
            Destroy(hurtMaterial);
        }
        
        StopAnimation();
    }
    
    void OnDisable()
    {
        // При отключении компонента возвращаем карточку в исходное состояние
        StopAnimation();
    }
}

