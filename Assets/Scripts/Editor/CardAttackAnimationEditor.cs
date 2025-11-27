using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Editor скрипт для предпросмотра анимации атаки карточки в редакторе Unity
/// </summary>
[CustomEditor(typeof(CardAttackAnimation))]
[CanEditMultipleObjects]
public class CardAttackAnimationEditor : Editor
{
    private CardAttackAnimation cardAttackAnimation;
    private bool isPreviewing = false;
    private float previewTime = 0f;
    private Material previewMaterial;
    private Material originalMaterial;
    private SpriteRenderer cachedSpriteRenderer;
    
    private void OnEnable()
    {
        cardAttackAnimation = (CardAttackAnimation)target;
        if (cardAttackAnimation != null)
        {
            cachedSpriteRenderer = cardAttackAnimation.GetComponent<SpriteRenderer>();
        }
    }
    
    private void OnDisable()
    {
        StopEditorPreview();
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        cardAttackAnimation = (CardAttackAnimation)target;
        
        if (cardAttackAnimation == null)
            return;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        
        // Кнопка для запуска в режиме Play
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        
        if (GUILayout.Button("Play Attack Animation (Runtime)"))
        {
            if (Application.isPlaying)
            {
                cardAttackAnimation.PlayAttackAnimation();
            }
        }
        
        EditorGUI.EndDisabledGroup();
        
        // Preview в редакторе (без запуска игры)
        EditorGUI.BeginDisabledGroup(isPreviewing);
        
        if (GUILayout.Button("Preview in Editor (No Play Mode)"))
        {
            StartEditorPreview();
        }
        
        EditorGUI.EndDisabledGroup();
        
        // Показываем элементы управления предпросмотром
        if (isPreviewing)
        {
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Stop Preview"))
            {
                StopEditorPreview();
            }
            
            EditorGUILayout.Space();
            
            // Слайдер для ручного управления временем анимации
            EditorGUI.BeginChangeCheck();
            float newPreviewTime = EditorGUILayout.Slider("Preview Time", previewTime, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                previewTime = newPreviewTime;
                UpdateEditorPreview();
                // Принудительно обновляем сцену
                SceneView.RepaintAll();
            }
            
            // Также обновляем при каждом перерисовке GUI, чтобы материал не сбрасывался
            UpdateEditorPreview();
            
            EditorGUILayout.Space();
            
            // Отладочная информация
            EditorGUILayout.LabelField("Debug Info:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Preview Time: {previewTime:F3}");
            EditorGUILayout.LabelField($"Material: {(previewMaterial != null ? previewMaterial.name : "NULL")}");
            EditorGUILayout.LabelField($"SpriteRenderer: {(cachedSpriteRenderer != null ? "Found" : "NULL")}");
            if (cachedSpriteRenderer != null)
            {
                EditorGUILayout.LabelField($"Current Material: {(cachedSpriteRenderer.sharedMaterial != null ? cachedSpriteRenderer.sharedMaterial.name : "NULL")}");
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Анимация предпросмотра активна. Используйте слайдер для просмотра разных фаз.", MessageType.Info);
        }
        
        // Информация о совместимости
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "✓ Этот компонент работает независимо от Animator.\n" +
            "✓ Он деформирует карточку через шейдер, в то время как Animator управляет спрайтами.\n" +
            "✓ Оба могут работать одновременно без конфликтов.\n\n" +
            "Для добавления в префаб UnitCardBase:\n" +
            "1. Откройте префаб UnitCardBase\n" +
            "2. Найдите объект CardSprite (внутри CardPivot)\n" +
            "3. Добавьте компонент CardAttackAnimation к CardSprite",
            MessageType.Info);
    }
    
    private void StartEditorPreview()
    {
        if (cardAttackAnimation == null)
        {
            Debug.LogWarning("CardAttackAnimationEditor: cardAttackAnimation is null!");
            return;
        }
        
        if (cachedSpriteRenderer == null)
        {
            cachedSpriteRenderer = cardAttackAnimation.GetComponent<SpriteRenderer>();
        }
        
        if (cachedSpriteRenderer == null)
        {
            Debug.LogWarning("CardAttackAnimationEditor: SpriteRenderer не найден!");
            EditorUtility.DisplayDialog("Ошибка", "SpriteRenderer не найден на объекте с CardAttackAnimation!", "OK");
            return;
        }
        
        // Сохраняем оригинальный материал
        originalMaterial = cachedSpriteRenderer.sharedMaterial;
        
        // Создаем материал для предпросмотра (используем универсальный шейдер)
        Shader attackShader = Shader.Find("Custom/CardUniversal");
        if (attackShader == null)
        {
            // Fallback на старый шейдер
            attackShader = Shader.Find("Custom/CardAttack");
            if (attackShader == null)
            {
                Debug.LogError("CardAttackAnimationEditor: Шейдер Custom/CardUniversal или Custom/CardAttack не найден! Убедитесь, что шейдер скомпилирован.");
                EditorUtility.DisplayDialog("Ошибка", "Шейдер не найден!", "OK");
                return;
            }
        }
        
        // Удаляем старый preview материал, если есть
        if (previewMaterial != null)
        {
            DestroyImmediate(previewMaterial);
            previewMaterial = null;
        }
        
        previewMaterial = new Material(attackShader);
        previewMaterial.name = "Preview_CardAttack_Material";
        
        // Копируем текстуру
        if (originalMaterial != null && originalMaterial.HasProperty("_MainTex"))
        {
            Texture mainTex = originalMaterial.GetTexture("_MainTex");
            if (mainTex != null)
            {
                previewMaterial.SetTexture("_MainTex", mainTex);
            }
        }
        
        if (cachedSpriteRenderer.sprite != null && cachedSpriteRenderer.sprite.texture != null)
        {
            if (!previewMaterial.HasProperty("_MainTex") || previewMaterial.GetTexture("_MainTex") == null)
            {
                previewMaterial.SetTexture("_MainTex", cachedSpriteRenderer.sprite.texture);
            }
        }
        
        // Применяем материал напрямую
        cachedSpriteRenderer.material = previewMaterial;
        
        isPreviewing = true;
        previewTime = 0f;
        
        // Сразу применяем начальное состояние
        UpdateEditorPreview();
        
        // Подписываемся на SceneView для обновления при перерисовке
        SceneView.duringSceneGui += OnSceneGUI;
        
        // Принудительно обновляем сцену и Inspector
        SceneView.RepaintAll();
        Repaint();
        
        Debug.Log("CardAttackAnimationEditor: Preview started. Material applied: " + (previewMaterial != null));
    }
    
    private void StopEditorPreview()
    {
        if (!isPreviewing)
            return;
            
        isPreviewing = false;
        SceneView.duringSceneGui -= OnSceneGUI;
        
        if (cachedSpriteRenderer == null && cardAttackAnimation != null)
        {
            cachedSpriteRenderer = cardAttackAnimation.GetComponent<SpriteRenderer>();
        }
        
        if (cachedSpriteRenderer != null)
        {
            // Сбрасываем параметры
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            cachedSpriteRenderer.GetPropertyBlock(propertyBlock);
            // Сбрасываем параметры для обоих вариантов шейдеров
            propertyBlock.SetFloat("_AttackSkewAmount", 0f);
            propertyBlock.SetFloat("_SkewAmount", 0f);
            cachedSpriteRenderer.SetPropertyBlock(propertyBlock);
            
            // Возвращаем оригинальный материал
            if (originalMaterial != null)
            {
                cachedSpriteRenderer.material = originalMaterial;
            }
            
            EditorUtility.SetDirty(cachedSpriteRenderer);
        }
        
        // Очищаем временный материал
        if (previewMaterial != null)
        {
            DestroyImmediate(previewMaterial);
            previewMaterial = null;
        }
        
        previewTime = 0f;
        
        // Принудительно обновляем сцену
        SceneView.RepaintAll();
        Repaint();
    }
    
    private void UpdateEditorPreview()
    {
        if (!isPreviewing || cardAttackAnimation == null)
        {
            return;
        }
        
        if (cachedSpriteRenderer == null)
        {
            cachedSpriteRenderer = cardAttackAnimation.GetComponent<SpriteRenderer>();
            if (cachedSpriteRenderer == null)
            {
                StopEditorPreview();
                return;
            }
        }
        
        // Убеждаемся, что материал создан
        if (previewMaterial == null)
        {
            Shader attackShader = Shader.Find("Custom/CardUniversal");
            if (attackShader == null)
            {
                attackShader = Shader.Find("Custom/CardAttack");
            }
            if (attackShader == null)
            {
                Debug.LogWarning("CardAttackAnimationEditor: Шейдер не найден!");
                StopEditorPreview();
                return;
            }
            
            previewMaterial = new Material(attackShader);
            previewMaterial.name = "Preview_CardAttack_Material";
            
            // Копируем текстуру
            if (originalMaterial != null && originalMaterial.HasProperty("_MainTex"))
            {
                previewMaterial.SetTexture("_MainTex", originalMaterial.GetTexture("_MainTex"));
            }
            else if (cachedSpriteRenderer.sprite != null)
            {
                previewMaterial.SetTexture("_MainTex", cachedSpriteRenderer.sprite.texture);
            }
        }
        
        // ВАЖНО: Применяем материал каждый раз, так как в редакторе он может сбрасываться
        if (cachedSpriteRenderer.sharedMaterial != previewMaterial)
        {
            cachedSpriteRenderer.material = previewMaterial;
        }
        
        // Получаем параметры напрямую
        float maxSkewAmount = cardAttackAnimation.maxSkewAmount;
        float forwardDuration = cardAttackAnimation.forwardDuration;
        float holdDuration = cardAttackAnimation.holdDuration;
        float returnDuration = cardAttackAnimation.returnDuration;
        
        float totalDuration = forwardDuration + holdDuration + returnDuration;
        float currentTime = previewTime * totalDuration;
        
        float skewAmount = 0f;
        
        if (totalDuration > 0f)
        {
            if (currentTime < forwardDuration)
            {
                // Фаза 1: Движение вправо
                float t = forwardDuration > 0f ? currentTime / forwardDuration : 0f;
                skewAmount = t * maxSkewAmount;
            }
            else if (currentTime < forwardDuration + holdDuration)
            {
                // Фаза 2: Застывание
                skewAmount = maxSkewAmount;
            }
            else
            {
                // Фаза 3: Возврат
                float t = returnDuration > 0f ? (currentTime - forwardDuration - holdDuration) / returnDuration : 1f;
                skewAmount = Mathf.Clamp01(1f - t) * maxSkewAmount;
            }
        }
        
        // Применяем деформацию через MaterialPropertyBlock
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        cachedSpriteRenderer.GetPropertyBlock(propertyBlock);
        // Используем правильное имя свойства для универсального шейдера
        if (previewMaterial != null && previewMaterial.shader.name == "Custom/CardUniversal")
        {
            propertyBlock.SetFloat("_AttackSkewAmount", skewAmount);
        }
        else
        {
            propertyBlock.SetFloat("_SkewAmount", skewAmount);
        }
        cachedSpriteRenderer.SetPropertyBlock(propertyBlock);
        
        // Помечаем объект как измененный для обновления в редакторе
        EditorUtility.SetDirty(cachedSpriteRenderer);
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        // Обновляем предпросмотр при перерисовке SceneView
        if (isPreviewing)
        {
            UpdateEditorPreview();
        }
    }
    
}

