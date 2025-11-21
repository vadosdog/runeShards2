using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Компонент для управления визуальными эффектами состояний юнита (горит, заморожен и т.д.)
/// Управляет ParticleSystem эффектами на карточке юнита
/// </summary>
public class UnitCardStatusEffects : MonoBehaviour
{
    [System.Serializable]
    public class StatusEffectMapping
    {
        public StatusType statusType;
        public ParticleSystem effectParticleSystem;
        public string effectObjectName; // Имя GameObject с эффектом (для поиска)
    }
    
    [Header("Status Effect Mappings")]
    [SerializeField] private List<StatusEffectMapping> statusEffectMappings = new List<StatusEffectMapping>();
    
    private Dictionary<StatusType, ParticleSystem> effectDictionary = new Dictionary<StatusType, ParticleSystem>();
    private HashSet<StatusType> activeEffects = new HashSet<StatusType>();
    
    void Awake()
    {
        InitializeEffectDictionary();
    }
    
    /// <summary>
    /// Инициализирует словарь эффектов из настроенных маппингов
    /// </summary>
    private void InitializeEffectDictionary()
    {
        effectDictionary.Clear();
        
        // Если маппинги не настроены, пытаемся найти эффекты по именам
        if (statusEffectMappings == null || statusEffectMappings.Count == 0)
        {
            AutoFindEffects();
        }
        else
        {
            // Используем настроенные маппинги
            foreach (var mapping in statusEffectMappings)
            {
                if (mapping.effectParticleSystem != null)
                {
                    effectDictionary[mapping.statusType] = mapping.effectParticleSystem;
                }
                else if (!string.IsNullOrEmpty(mapping.effectObjectName))
                {
                    // Ищем по имени, если ParticleSystem не назначен напрямую
                    Transform effectTransform = transform.Find(mapping.effectObjectName);
                    if (effectTransform != null)
                    {
                        ParticleSystem ps = effectTransform.GetComponent<ParticleSystem>();
                        if (ps != null)
                        {
                            effectDictionary[mapping.statusType] = ps;
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Автоматически находит эффекты по стандартным именам
    /// </summary>
    private void AutoFindEffects()
    {
        // Стандартные имена эффектов
        Dictionary<string, StatusType> standardNames = new Dictionary<string, StatusType>
        {
            { "BurningEffect", StatusType.Burning },
            { "FrozenEffect", StatusType.Slowed }, // Можно добавить Frozen в enum позже
            { "PoisonedEffect", StatusType.Poisoned },
            { "StunnedEffect", StatusType.Stunned },
            { "WetEffect", StatusType.Wet },
            { "ShieldedEffect", StatusType.Shielded },
            { "EmpoweredEffect", StatusType.Empowered }
        };
        
        foreach (var kvp in standardNames)
        {
            Transform effectTransform = transform.Find(kvp.Key);
            if (effectTransform != null)
            {
                ParticleSystem ps = effectTransform.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    effectDictionary[kvp.Value] = ps;
                    // Отключаем эффект по умолчанию
                    ps.Stop();
                    var main = ps.main;
                    main.playOnAwake = false;
                }
            }
        }
    }
    
    /// <summary>
    /// Устанавливает эффект статуса (включает или выключает)
    /// </summary>
    public void SetStatusEffect(StatusType status, bool active)
    {
        if (!effectDictionary.ContainsKey(status))
        {
            // Эффект не найден - это нормально, не все статусы имеют визуальные эффекты
            return;
        }
        
        ParticleSystem effect = effectDictionary[status];
        if (effect == null)
            return;
        
        if (active)
        {
            if (!activeEffects.Contains(status))
            {
                activeEffects.Add(status);
                effect.Play();
            }
        }
        else
        {
            if (activeEffects.Contains(status))
            {
                activeEffects.Remove(status);
                effect.Stop();
            }
        }
    }
    
    /// <summary>
    /// Очищает все активные эффекты статусов
    /// </summary>
    public void ClearAllStatusEffects()
    {
        foreach (var status in activeEffects)
        {
            if (effectDictionary.ContainsKey(status))
            {
                ParticleSystem effect = effectDictionary[status];
                if (effect != null)
                {
                    effect.Stop();
                }
            }
        }
        activeEffects.Clear();
    }
    
    /// <summary>
    /// Проверяет, активен ли эффект статуса
    /// </summary>
    public bool IsStatusEffectActive(StatusType status)
    {
        return activeEffects.Contains(status);
    }
    
    /// <summary>
    /// Получает список активных эффектов
    /// </summary>
    public HashSet<StatusType> GetActiveEffects()
    {
        return new HashSet<StatusType>(activeEffects);
    }
}

