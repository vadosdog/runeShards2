using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Компонент для управления плашкой здоровья над карточкой юнита
/// Отображает имя и здоровье в виде сегментов (как в Wildermyth)
/// </summary>
public class UnitHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private Transform healthSegmentsContainer;
    [SerializeField] private GameObject healthSegmentPrefab;
    [SerializeField] private RectTransform canvasRectTransform; // RectTransform Canvas для изменения размера
    
    [Header("Layout Settings")]
    [SerializeField] private float baseCanvasWidth = 200f; // Базовая ширина Canvas (для <=20 здоровья)
    [SerializeField] private float extendedCanvasWidth = 300f; // Расширенная ширина Canvas (для 21-30 здоровья)
    [SerializeField] private float segmentSize = 8f; // Размер одного сегмента
    [SerializeField] private float segmentSpacing = 1f; // Отступ между сегментами
    
    [Header("Health Colors")]
    [SerializeField] private Color healthyColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Зеленый (>66%)
    [SerializeField] private Color mediumColor = new Color(0.8f, 0.8f, 0.2f, 1f); // Желтый (33-66%)
    [SerializeField] private Color lowColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Красный (<33%)
    [SerializeField] private Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Серый для потерянного здоровья
    
    [Header("Animation Settings")]
    [SerializeField] private float maxTotalAnimationDuration = 1f; // Максимальная общая длительность анимации (1 секунда)
    [SerializeField] private float baseSegmentAnimationDuration = 0.2f; // Базовая длительность анимации одного сегмента
    [SerializeField] private float baseDelayBetweenSegments = 0.05f; // Базовая задержка между сегментами
    
    private List<Image> healthSegments = new List<Image>();
    private int currentMaxHealth = 0;
    private int currentDisplayedHealth = 0;
    private Coroutine updateCoroutine;
    private bool useTwoRows = false; // Используются ли два ряда
    private HorizontalLayoutGroup horizontalLayout;
    private GridLayoutGroup gridLayout;
    
    private Camera mainCamera;
    
    void Awake()
    {
        // Ищем компоненты, если не назначены
        if (unitNameText == null)
        {
            unitNameText = transform.GetComponentInChildren<TMP_Text>();
        }
        
        if (healthSegmentsContainer == null)
        {
            healthSegmentsContainer = transform.Find("HealthBarBackground/HealthSegmentsContainer");
            if (healthSegmentsContainer == null)
            {
                // Пытаемся найти рекурсивно
                healthSegmentsContainer = FindInChildren(transform, "HealthSegmentsContainer");
            }
        }
        
        // Получаем RectTransform Canvas
        if (canvasRectTransform == null)
        {
            canvasRectTransform = GetComponent<RectTransform>();
        }
        
        // Получаем компоненты Layout
        if (healthSegmentsContainer != null)
        {
            horizontalLayout = healthSegmentsContainer.GetComponent<HorizontalLayoutGroup>();
            gridLayout = healthSegmentsContainer.GetComponent<GridLayoutGroup>();
        }
        
        // Загружаем префаб сегмента, если не назначен
        if (healthSegmentPrefab == null)
        {
            healthSegmentPrefab = Resources.Load<GameObject>("HealthSegment");
            if (healthSegmentPrefab == null)
            {
                // Пытаемся найти через AssetDatabase (только в Editor)
                #if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("HealthSegment t:Prefab");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    healthSegmentPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
                #endif
            }
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
    }
    
    void LateUpdate()
    {
        // Поворачиваем плашку к камере
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
        }
    }
    
    /// <summary>
    /// Инициализирует плашку здоровья для юнита
    /// </summary>
    /// <param name="unitName">Имя юнита</param>
    /// <param name="maxHealth">Максимальное здоровье</param>
    /// <param name="currentHealth">Текущее здоровье</param>
    public void Initialize(string unitName, int maxHealth, int currentHealth)
    {
        // Устанавливаем имя
        if (unitNameText != null)
        {
            unitNameText.text = unitName;
        }
        
        // Создаем сегменты здоровья
        CreateHealthSegments(maxHealth);
        
        // Обновляем отображение здоровья
        UpdateHealth(currentHealth, false); // Без анимации при инициализации
    }
    
    /// <summary>
    /// Обновляет отображение здоровья
    /// </summary>
    /// <param name="newHealth">Новое значение здоровья</param>
    /// <param name="animate">Анимировать ли изменение</param>
    public void UpdateHealth(int newHealth, bool animate = true)
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
        
        if (animate)
        {
            updateCoroutine = StartCoroutine(AnimateHealthChange(newHealth));
        }
        else
        {
            currentDisplayedHealth = newHealth;
            RefreshHealthSegments();
        }
    }
    
    /// <summary>
    /// Создает сегменты здоровья
    /// </summary>
    private void CreateHealthSegments(int maxHealth)
    {
        if (healthSegmentsContainer == null || healthSegmentPrefab == null)
        {
            Debug.LogError("UnitHealthBar: healthSegmentsContainer или healthSegmentPrefab не назначены!");
            return;
        }
        
        // Очищаем существующие сегменты
        ClearHealthSegments();
        
        currentMaxHealth = maxHealth;
        
        // Определяем, нужны ли два ряда
        useTwoRows = maxHealth > 30;
        
        // Настраиваем размер Canvas и Layout
        SetupCanvasAndLayout(maxHealth);
        
        // Создаем новые сегменты
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject segmentObj = Instantiate(healthSegmentPrefab, healthSegmentsContainer);
            Image segmentImage = segmentObj.GetComponent<Image>();
            
            if (segmentImage == null)
            {
                segmentImage = segmentObj.AddComponent<Image>();
            }
            
            // Настраиваем размер сегмента
            RectTransform segmentRect = segmentObj.GetComponent<RectTransform>();
            if (segmentRect != null)
            {
                segmentRect.sizeDelta = new Vector2(segmentSize, segmentSize);
            }
            
            healthSegments.Add(segmentImage);
        }
    }
    
    /// <summary>
    /// Настраивает размер Canvas и Layout в зависимости от количества здоровья
    /// </summary>
    private void SetupCanvasAndLayout(int maxHealth)
    {
        if (canvasRectTransform == null || healthSegmentsContainer == null)
            return;
        
        // Настраиваем Layout компоненты
        if (useTwoRows)
        {
            // Используем GridLayoutGroup для двух рядов
            if (gridLayout == null)
            {
                // Удаляем HorizontalLayoutGroup, если есть
                if (horizontalLayout != null)
                {
                    if (Application.isPlaying)
                        Destroy(horizontalLayout);
                    else
                        DestroyImmediate(horizontalLayout);
                    horizontalLayout = null;
                }
                
                // Создаем GridLayoutGroup
                gridLayout = healthSegmentsContainer.gameObject.AddComponent<GridLayoutGroup>();
            }
            
            // Настраиваем GridLayoutGroup
            gridLayout.cellSize = new Vector2(segmentSize, segmentSize);
            gridLayout.spacing = new Vector2(segmentSpacing, segmentSpacing);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            
            // Используем фиксированное количество строк (2 ряда)
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            gridLayout.constraintCount = 2;
            
            // Вычисляем количество столбцов (сегментов в одном ряду)
            int columns = Mathf.CeilToInt((float)maxHealth / 2f);
            // Ограничиваем максимумом, но не меньше чем нужно для размещения всех сегментов
            columns = Mathf.Max(columns, Mathf.CeilToInt((float)maxHealth / 2f));
            
            // Настраиваем размер Canvas для двух рядов
            // Ширина = количество столбцов * (размер сегмента + отступ) + отступ
            float calculatedWidth = columns * (segmentSize + segmentSpacing) + segmentSpacing;
            // Высота = 2 ряда * (размер сегмента + отступ) + отступ
            float calculatedHeight = 2f * (segmentSize + segmentSpacing) + segmentSpacing;
            canvasRectTransform.sizeDelta = new Vector2(calculatedWidth, calculatedHeight);
        }
        else
        {
            // Используем HorizontalLayoutGroup для одного ряда
            if (horizontalLayout == null)
            {
                // Удаляем GridLayoutGroup, если есть
                if (gridLayout != null)
                {
                    if (Application.isPlaying)
                        Destroy(gridLayout);
                    else
                        DestroyImmediate(gridLayout);
                    gridLayout = null;
                }
                
                // Создаем HorizontalLayoutGroup
                horizontalLayout = healthSegmentsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            
            // Настраиваем HorizontalLayoutGroup
            horizontalLayout.spacing = segmentSpacing;
            horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childControlHeight = false;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;
            
            // Настраиваем размер Canvas
            if (maxHealth <= 20)
            {
                canvasRectTransform.sizeDelta = new Vector2(baseCanvasWidth, canvasRectTransform.sizeDelta.y);
            }
            else // 21-30 здоровья
            {
                canvasRectTransform.sizeDelta = new Vector2(extendedCanvasWidth, canvasRectTransform.sizeDelta.y);
            }
        }
    }
    
    /// <summary>
    /// Очищает все сегменты здоровья
    /// </summary>
    private void ClearHealthSegments()
    {
        foreach (Image segment in healthSegments)
        {
            if (segment != null && segment.gameObject != null)
            {
                Destroy(segment.gameObject);
            }
        }
        healthSegments.Clear();
    }
    
    /// <summary>
    /// Обновляет цвета сегментов здоровья
    /// </summary>
    private void RefreshHealthSegments()
    {
        if (healthSegments.Count == 0)
            return;
        
        float healthPercent = currentMaxHealth > 0 ? (float)currentDisplayedHealth / currentMaxHealth : 0f;
        Color activeColor = GetHealthColor(healthPercent);
        
        for (int i = 0; i < healthSegments.Count; i++)
        {
            if (healthSegments[i] == null)
                continue;
            
            // Первые currentDisplayedHealth сегментов - цветные, остальные - серые
            if (i < currentDisplayedHealth)
            {
                healthSegments[i].color = activeColor;
            }
            else
            {
                healthSegments[i].color = emptyColor;
            }
        }
    }
    
    /// <summary>
    /// Получает цвет здоровья в зависимости от процента
    /// </summary>
    private Color GetHealthColor(float healthPercent)
    {
        if (healthPercent > 0.66f)
            return healthyColor;
        else if (healthPercent > 0.33f)
            return mediumColor;
        else
            return lowColor;
    }
    
    /// <summary>
    /// Анимирует изменение здоровья
    /// </summary>
    private IEnumerator AnimateHealthChange(int targetHealth)
    {
        int startHealth = currentDisplayedHealth;
        int healthDiff = targetHealth - startHealth;
        
        if (healthDiff == 0)
            yield break;
        
        // Определяем направление изменения
        bool isHealing = healthDiff > 0;
        int steps = Mathf.Abs(healthDiff);
        
        // Вычисляем динамические параметры анимации, чтобы общее время не превышало maxTotalAnimationDuration
        // Общее время = (количество шагов - 1) * задержка + длительность анимации последнего сегмента
        // Упрощенная формула: steps * (delay + duration) - delay
        float estimatedTotalTime = steps * (baseDelayBetweenSegments + baseSegmentAnimationDuration) - baseDelayBetweenSegments;
        
        float actualSegmentDuration = baseSegmentAnimationDuration;
        float actualDelay = baseDelayBetweenSegments;
        
        // Если расчетное время больше максимума, пропорционально уменьшаем параметры
        if (estimatedTotalTime > maxTotalAnimationDuration)
        {
            float scaleFactor = maxTotalAnimationDuration / estimatedTotalTime;
            actualSegmentDuration = baseSegmentAnimationDuration * scaleFactor;
            actualDelay = baseDelayBetweenSegments * scaleFactor;
            
            // Минимальные значения для плавности анимации
            actualSegmentDuration = Mathf.Max(actualSegmentDuration, 0.05f);
            actualDelay = Mathf.Max(actualDelay, 0.01f);
        }
        
        // Анимируем каждый сегмент по очереди
        for (int step = 0; step < steps; step++)
        {
            int segmentIndex;
            if (isHealing)
            {
                // При лечении: добавляем сегменты снизу вверх
                segmentIndex = startHealth + step;
            }
            else
            {
                // При уроне: убираем сегменты сверху вниз
                segmentIndex = startHealth - step - 1;
            }
            
            // Проверяем границы
            if (segmentIndex < 0 || segmentIndex >= healthSegments.Count)
                continue;
            
            // Анимируем изменение цвета сегмента
            Image segment = healthSegments[segmentIndex];
            if (segment != null)
            {
                Color startColor = segment.color;
                int newHealth = isHealing ? startHealth + step + 1 : startHealth - step - 1;
                Color targetColor = isHealing ? GetHealthColor((float)newHealth / currentMaxHealth) : emptyColor;
                
                float elapsed = 0f;
                while (elapsed < actualSegmentDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / actualSegmentDuration);
                    segment.color = Color.Lerp(startColor, targetColor, t);
                    yield return null;
                }
                
                segment.color = targetColor;
            }
            
            // Обновляем текущее отображаемое здоровье
            currentDisplayedHealth = isHealing ? startHealth + step + 1 : startHealth - step - 1;
            
            // Задержка между сегментами
            if (step < steps - 1)
            {
                yield return new WaitForSeconds(actualDelay);
            }
        }
        
        // Финальное обновление всех сегментов (на случай, если изменился процент здоровья)
        RefreshHealthSegments();
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
    
    void OnDestroy()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }
}

