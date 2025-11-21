using UnityEngine;

/// <summary>
/// Компонент для управления подсветкой юнита по контуру спрайта
/// Меняет цвет подсветки в зависимости от команды и активности юнита
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class UnitCardHighlight : MonoBehaviour
{
    [Header("Highlight Colors")]
    [SerializeField] private Color team1InactiveColor = new Color(0.0f, 0.2f, 0.5f, 1.0f); // Темно-синий
    [SerializeField] private Color team1ActiveColor = new Color(0.2f, 0.8f, 1.0f, 1.0f); // Ярко-голубой
    [SerializeField] private Color team2InactiveColor = new Color(0.5f, 0.0f, 0.0f, 1.0f); // Темно-красный
    [SerializeField] private Color team2ActiveColor = new Color(1.0f, 0.2f, 0.2f, 1.0f); // Ярко-красный
    
    [Header("Highlight Settings")]
    [SerializeField] private float outlineWidth = 3.0f;
    [SerializeField] private float outlineGlow = 2.0f;
    [SerializeField] private float pulseSpeed = 2.0f; // Скорость пульсации для активных юнитов
    [SerializeField] private float pulseMinIntensity = 0.8f;
    [SerializeField] private float pulseMaxIntensity = 1.2f;
    
    private SpriteRenderer spriteRenderer;
    private Material outlineMaterial;
    private bool isTeam1 = true;
    private bool isActive = false;
    private float pulseTime = 0f;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CreateOutlineMaterial();
    }
    
    void OnEnable()
    {
        if (spriteRenderer != null && outlineMaterial != null)
        {
            spriteRenderer.material = outlineMaterial;
        }
    }
    
    void OnDisable()
    {
        // Восстанавливаем оригинальный материал при отключении
        if (spriteRenderer != null)
        {
            spriteRenderer.material = spriteRenderer.sharedMaterial;
        }
    }
    
    void Update()
    {
        // Обновляем пульсацию только для активных юнитов
        if (isActive && outlineMaterial != null)
        {
            pulseTime += Time.deltaTime * pulseSpeed;
            float pulseIntensity = Mathf.Lerp(pulseMinIntensity, pulseMaxIntensity, 
                (Mathf.Sin(pulseTime) + 1f) * 0.5f);
            
            // Применяем пульсацию к свечению
            outlineMaterial.SetFloat("_OutlineGlow", outlineGlow * pulseIntensity);
        }
        // Для неактивных юнитов пульсация не нужна - значение уже установлено в UpdateHighlightColor()
    }
    
    /// <summary>
    /// Создает материал для подсветки
    /// </summary>
    private void CreateOutlineMaterial()
    {
        // Загружаем шейдер
        Shader outlineShader = Shader.Find("Custom/SpriteOutline");
        if (outlineShader == null)
        {
            Debug.LogError("Shader 'Custom/SpriteOutline' not found! Make sure the shader is in the project.");
            return;
        }
        
        // Создаем материал из шейдера
        outlineMaterial = new Material(outlineShader);
        
        // Копируем текстуру из оригинального материала
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            outlineMaterial.mainTexture = spriteRenderer.sprite.texture;
        }
        
        // Устанавливаем параметры подсветки
        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineMaterial.SetFloat("_OutlineGlow", outlineGlow);
        
        // Применяем материал к SpriteRenderer
        if (spriteRenderer != null)
        {
            spriteRenderer.material = outlineMaterial;
        }
        
        UpdateHighlightColor();
    }
    
    /// <summary>
    /// Устанавливает команду юнита (true = команда 1, false = команда 2)
    /// </summary>
    public void SetTeam(bool isTeam1Unit)
    {
        isTeam1 = isTeam1Unit;
        UpdateHighlightColor();
    }
    
    /// <summary>
    /// Устанавливает активность юнита
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
        UpdateHighlightColor();
    }
    
    /// <summary>
    /// Обновляет цвет подсветки в зависимости от команды и активности
    /// </summary>
    private void UpdateHighlightColor()
    {
        if (outlineMaterial == null)
            return;
        
        Color highlightColor;
        
        if (isTeam1)
        {
            // Команда 1
            highlightColor = isActive ? team1ActiveColor : team1InactiveColor;
        }
        else
        {
            // Команда 2
            highlightColor = isActive ? team2ActiveColor : team2InactiveColor;
        }
        
        outlineMaterial.SetColor("_OutlineColor", highlightColor);
    }
    
    /// <summary>
    /// Отключает подсветку
    /// </summary>
    public void DisableHighlight()
    {
        if (outlineMaterial != null)
        {
            outlineMaterial.SetFloat("_OutlineGlow", 0f);
        }
    }
    
    /// <summary>
    /// Включает подсветку
    /// </summary>
    public void EnableHighlight()
    {
        if (outlineMaterial != null)
        {
            outlineMaterial.SetFloat("_OutlineGlow", outlineGlow);
            UpdateHighlightColor();
        }
    }
    
    void OnDestroy()
    {
        // Освобождаем ресурсы материала
        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);
        }
    }
}

