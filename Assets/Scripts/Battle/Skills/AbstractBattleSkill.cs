using UnityEngine;
using System.Collections.Generic;

public abstract class AbstractBattleSkill : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public string description;
    public Sprite icon;

    [Header("Cost & Requirements")]
    public int staminaCost;
    public int cooldown;

    [Header("Range & Targeting")]
    public SkillTargetType targetType;
    public int minRange;
    public int maxRange;
    public bool requiresLineOfSight; // Получается ли штраф за прикрытия

    [Header("Skill Zones")]
    public List<SkillEffectZone> zones; // Каждая зона имеет свой pattern и набор эффектов

    [Header("Visualization")]
    [Tooltip("Префаб снаряда, который летит от кастера к цели (опционально, только для дальних атак)")]
    public GameObject projectilePrefab;
    
    // Константы для визуализации
    private const float PROJECTILE_FLIGHT_DURATION = 0.5f;
    private const float EFFECT_DURATION = 0.5f;
    
    // Константы для позиционирования
    private const float PROJECTILE_START_HEIGHT = 1.5f;
    private const float PROJECTILE_TARGET_HEIGHT = 3.0f;
    private const float EFFECT_HEIGHT_OFFSET = 3.0f;
    private const float Z_OFFSET_FORWARD = 0.5f;
    private const float PROJECTILE_SCALE = 10f;
    private const float EFFECT_SCALE = 10f;
    private const float MELEE_ATTACK_DISTANCE_THRESHOLD = 1.5f;
    private const float MELEE_EFFECT_OFFSET_RATIO = 0.3f;
    private const int EFFECT_SORTING_ORDER = 200;


    // Основные методы которые нужно реализовать
    public abstract bool IsValidTarget(HexCell targetCell, BattleHexUnit caster);
    public abstract void ShowTargetingPreview(HexCell targetCell, BattleHexUnit caster);
    public abstract void HideTargetingPreview();
    public abstract SkillResult Execute(HexCell targetCell, BattleHexUnit caster);
    
    /// <summary>
    /// Выполняет навык с визуализацией (асинхронно через корутину)
    /// </summary>
    public System.Collections.IEnumerator ExecuteWithVisualization(HexCell targetCell, BattleHexUnit caster, System.Action<SkillResult> onComplete)
    {
        SkillResult result = new SkillResult
        {
            success = false,
            appliedEffects = new List<SkillEffectApplication>()
        };

        if (!IsValidTarget(targetCell, caster))
        {
            Debug.LogWarning($"Недопустимая цель для навыка {skillName}");
            onComplete?.Invoke(result);
            yield break;
        }

        // Проверяем стоимость стамины
        if (caster.currentStamina < staminaCost)
        {
            Debug.LogWarning($"Недостаточно стамины для использования {skillName}");
            onComplete?.Invoke(result);
            yield break;
        }

        // Проверяем наличие зон
        if (zones == null || zones.Count == 0)
        {
            Debug.LogWarning($"У навыка {skillName} нет настроенных зон");
            onComplete?.Invoke(result);
            yield break;
        }

        // Если есть префаб снаряда, запускаем его полет
        if (projectilePrefab != null)
        {
            yield return PlayProjectileVisualization(targetCell, caster);
        }

        // Для ближних атак: применяем урон сразу (чтобы анимация hurt запустилась с задержкой 0.1s)
        // и запускаем эффекты параллельно
        bool isMeleeAttack = this is BaseMeleeBattleSkill;
        if (isMeleeAttack)
        {
            // Применяем эффекты сразу (урон вызовет анимацию hurt)
            result = ApplySkillEffects(targetCell, caster);
            
            // Запускаем визуализацию эффектов и ждем завершения
            yield return PlayZoneEffectsVisualization(targetCell, caster);
        }
        else
        {
            // Для дальних атак: сначала визуализация, потом эффекты
            yield return PlayZoneEffectsVisualization(targetCell, caster);
            
            // После завершения визуализации применяем игровые эффекты
            result = ApplySkillEffects(targetCell, caster);
        }

        // Тратим стамину
        if (result.success)
        {
            caster.ConsumeStamina(staminaCost);
            Debug.Log($"{caster.name} использовал {skillName} на {targetCell.Coordinates}");
        }

        onComplete?.Invoke(result);
    }

    /// <summary>
    /// Получает ссылку на HexGrid через кастера или ищет в сцене
    /// </summary>
    private HexGrid GetHexGrid(BattleHexUnit caster)
    {
        if (caster != null && caster.Grid != null)
        {
            return caster.Grid;
        }
        return Object.FindFirstObjectByType<HexGrid>();
    }

    /// <summary>
    /// Воспроизводит визуализацию полета снаряда
    /// </summary>
    private System.Collections.IEnumerator PlayProjectileVisualization(HexCell targetCell, BattleHexUnit caster)
    {
        if (projectilePrefab == null || caster == null || targetCell == null)
            yield break;

        // Получаем позиции
        Vector3 startPos = caster.transform.position;
        Vector3 targetPos = targetCell.Position;
        
        // Преобразуем локальную позицию ячейки в мировую
        HexGrid grid = GetHexGrid(caster);
        if (grid != null && grid.transform != null)
        {
            targetPos = grid.transform.TransformPoint(targetPos);
        }
        
        // Добавляем смещение вверх для старта и цели снаряда
        // Position уже включает elevation, поэтому добавляем к существующему Y
        startPos.y = startPos.y + PROJECTILE_START_HEIGHT;
        targetPos.y = targetPos.y + PROJECTILE_TARGET_HEIGHT;
        
        // Смещаем снаряд вперед по оси Z, чтобы он был виден перед карточками юнитов
        startPos.z -= Z_OFFSET_FORWARD;
        targetPos.z -= Z_OFFSET_FORWARD;

        // Создаем снаряд
        GameObject projectileInstance = Instantiate(projectilePrefab, startPos, Quaternion.LookRotation(targetPos - startPos));
        
        // Увеличиваем масштаб снаряда
        projectileInstance.transform.localScale = Vector3.one * PROJECTILE_SCALE;
        
        // Получаем или добавляем ProjectileController
        ProjectileController projectileController = projectileInstance.GetComponent<ProjectileController>();
        if (projectileController == null)
        {
            projectileController = projectileInstance.AddComponent<ProjectileController>();
        }

        // Запускаем полет снаряда
        yield return projectileController.FlyToTarget(startPos, targetPos, PROJECTILE_FLIGHT_DURATION);

        // Уничтожаем снаряд после завершения
        Destroy(projectileInstance);
    }

    /// <summary>
    /// Воспроизводит визуализацию эффектов всех зон
    /// </summary>
    private System.Collections.IEnumerator PlayZoneEffectsVisualization(HexCell targetCell, BattleHexUnit caster)
    {
        if (zones == null || zones.Count == 0)
            yield break;

        List<GameObject> effectInstances = new List<GameObject>();
        
        // Получаем позицию ячейки
        // Position возвращает локальную позицию относительно grid, нужно преобразовать в мировую
        Vector3 targetPos = targetCell.Position;
        
        // Получаем grid через кастера (если есть) или ищем в сцене
        HexGrid grid = GetHexGrid(caster);
        
        // Преобразуем локальную позицию в мировую
        if (grid != null && grid.transform != null)
        {
            targetPos = grid.transform.TransformPoint(targetPos);
        }
        
        // Добавляем смещение вверх с учетом высоты ячейки
        // Position уже включает elevation (из RefreshCellPosition), поэтому просто добавляем смещение вверх
        targetPos.y = targetPos.y + EFFECT_HEIGHT_OFFSET;

        // Создаем эффекты для всех зон
        foreach (var zone in zones)
        {
            if (zone == null || zone.effectPrefab == null)
                continue;

            // Вычисляем параметры для размещения эффекта
            Vector3 casterPos = caster != null && caster.transform != null ? caster.transform.position : Vector3.zero;
            Vector3 direction = CalculateEffectDirection(caster, casterPos, targetPos, out float distance);
            float horizontalDistance = CalculateHorizontalDistance(casterPos, targetPos);
            
            // Вычисляем позицию и поворот эффекта
            Vector3 effectPosition = CalculateEffectPosition(casterPos, targetPos, direction, horizontalDistance);
            Quaternion effectRotation = CalculateEffectRotation(direction, horizontalDistance);
            
            // Создаем и настраиваем экземпляр эффекта
            GameObject effectInstance = CreateEffectInstance(zone.effectPrefab, effectPosition, effectRotation, distance);
            effectInstances.Add(effectInstance);

            // Запускаем эффект без ограничения по времени
            // Эффект будет играть до тех пор, пока ParticleSystem не завершится сам
            SkillEffectController effectController = effectInstance.GetComponent<SkillEffectController>();
            if (effectController == null)
            {
                effectController = effectInstance.AddComponent<SkillEffectController>();
            }
            effectController.PlayEffect();
        }

        // Не ждем завершения эффектов и не уничтожаем их автоматически
        // Эффекты будут играть до тех пор, пока ParticleSystem не завершится сам
        // или пока объект не будет уничтожен вручную
    }

    /// <summary>
    /// Применяет игровые эффекты навыка (урон, лечение и т.д.)
    /// </summary>
    private SkillResult ApplySkillEffects(HexCell targetCell, BattleHexUnit caster)
    {
        SkillResult result = new SkillResult
        {
            success = true,
            appliedEffects = new List<SkillEffectApplication>()
        };

        // Применяем эффекты из всех зон
        foreach (var zone in zones)
        {
            if (zone.pattern == null || zone.effects == null)
                continue;

            // Получаем затронутые ячейки по паттерну
            List<HexCell> affectedCells = zone.pattern.GetAffectedCells(targetCell, caster);

            foreach (var cell in affectedCells)
            {
                if (cell == null)
                    continue;

                // Находим юнита на ячейке
                HexUnit unitOnCell = cell.Unit;
                BattleHexUnit targetUnit = unitOnCell as BattleHexUnit;

                // Применяем эффекты
                foreach (var effect in zone.effects)
                {
                    if (effect == null)
                        continue;

                    int damageDealt = 0;
                    int healingAmount = 0;

                    // Если эффект наносит урон, вычисляем его
                    if (effect is DealDamageEffect && targetUnit != null)
                    {
                        var damageResult = BattleCalculator.CalculateDamage(
                            effect, caster, targetUnit, new BattleContext());
                        damageDealt = damageResult.damageAmount;
                    }

                    // Если эффект лечит, вычисляем количество лечения
                    if (effect is HealEffect && targetUnit != null)
                    {
                        var healResult = BattleCalculator.CalculateHealing(
                            effect, caster, targetUnit, new BattleContext());
                        healingAmount = healResult.healingAmount;
                    }

                    // Применяем эффект
                    effect.Apply(caster, targetUnit, cell);

                    // Записываем результат применения
                    result.appliedEffects.Add(new SkillEffectApplication
                    {
                        effect = effect,
                        target = targetUnit,
                        cell = cell,
                        damageDealt = damageDealt > 0 ? damageDealt : -healingAmount // Отрицательное для лечения
                    });
                }
            }
        }

        return result;
    }
    
    /// <summary>
    /// Вычисляет направление от кастера к цели для правильной ориентации эффекта
    /// </summary>
    private Vector3 CalculateEffectDirection(BattleHexUnit caster, Vector3 casterPos, Vector3 targetPos, out float distance)
    {
        distance = 0f;
        
        if (caster == null || caster.transform == null)
            return Vector3.zero;
        
        // Выравниваем высоту кастера с высотой цели для правильного расчета направления
        Vector3 casterPosForDirection = new Vector3(casterPos.x, targetPos.y, casterPos.z);
        Vector3 fullDirection = targetPos - casterPosForDirection;
        distance = fullDirection.magnitude;
        
        if (distance > 0.001f)
        {
            return fullDirection.normalized;
        }
        
        // Если направление нулевое (атака на себя), используем направление вперед кастера
        return caster.transform.forward;
    }
    
    /// <summary>
    /// Вычисляет горизонтальное расстояние между кастером и целью
    /// </summary>
    private float CalculateHorizontalDistance(Vector3 casterPos, Vector3 targetPos)
    {
        return Vector3.Distance(
            new Vector3(casterPos.x, 0f, casterPos.z),
            new Vector3(targetPos.x, 0f, targetPos.z)
        );
    }
    
    /// <summary>
    /// Вычисляет позицию эффекта с учетом типа атаки (ближняя/дальняя)
    /// </summary>
    private Vector3 CalculateEffectPosition(Vector3 casterPos, Vector3 targetPos, Vector3 direction, float horizontalDistance)
    {
        Vector3 effectPosition = targetPos;
        
        // Если это ближняя атака, размещаем эффект между атакующим и целью
        if (horizontalDistance <= MELEE_ATTACK_DISTANCE_THRESHOLD && direction != Vector3.zero)
        {
            float effectOffset = horizontalDistance * MELEE_EFFECT_OFFSET_RATIO;
            Vector3 horizontalDirection = new Vector3(direction.x, 0f, direction.z).normalized;
            Vector3 casterPosAligned = new Vector3(casterPos.x, targetPos.y, casterPos.z);
            effectPosition = casterPosAligned + horizontalDirection * effectOffset;
        }
        
        // Смещаем эффект вперед по оси Z, чтобы он был виден перед карточками юнитов
        effectPosition.z -= Z_OFFSET_FORWARD;
        
        return effectPosition;
    }
    
    /// <summary>
    /// Вычисляет поворот эффекта с учетом типа атаки
    /// </summary>
    private Quaternion CalculateEffectRotation(Vector3 direction, float horizontalDistance)
    {
        Quaternion effectRotation = direction != Vector3.zero 
            ? Quaternion.LookRotation(direction)
            : Quaternion.identity;
        
        // Для ближних атак инвертируем направление
        if (horizontalDistance <= MELEE_ATTACK_DISTANCE_THRESHOLD && direction != Vector3.zero)
        {
            effectRotation = Quaternion.LookRotation(-direction);
        }
        
        return effectRotation;
    }
    
    /// <summary>
    /// Создает и настраивает экземпляр эффекта
    /// </summary>
    private GameObject CreateEffectInstance(GameObject effectPrefab, Vector3 position, Quaternion rotation, float distance)
    {
        GameObject effectInstance = Instantiate(effectPrefab, position, rotation);
        effectInstance.transform.localScale = Vector3.one * EFFECT_SCALE;
        SetEffectSortingLayer(effectInstance, distance <= MELEE_ATTACK_DISTANCE_THRESHOLD);
        return effectInstance;
    }
    
    /// <summary>
    /// Устанавливает Sorting Order для эффекта, чтобы он был виден перед карточками юнитов
    /// </summary>
    private void SetEffectSortingLayer(GameObject effectInstance, bool isMeleeAttack)
    {
        // Для ближних атак особенно важно быть видимыми
        // Увеличиваем sorting order для всех рендереров в эффекте
        Renderer[] renderers = effectInstance.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                // Устанавливаем высокий sorting order, чтобы эффект был перед карточками
                renderer.sortingOrder = EFFECT_SORTING_ORDER;
            }
        }
        
        // Для ParticleSystem также устанавливаем sorting order
        ParticleSystem[] particleSystems = effectInstance.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                var renderer = ps.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = EFFECT_SORTING_ORDER;
                }
            }
        }
    }
}

public enum SkillTargetType
{
    EnemyUnit,
    AllyUnit,
    AnyUnit,
    EmptyCell,
    AnyCell,
    // Добавить для ячеек с предметами
}

public enum EffectCategory { Physical, Magical, Status, Utility }

public struct SkillResult
{
    public bool success;
    public List<SkillEffectApplication> appliedEffects;
}

public struct SkillEffectApplication
{
    public AbstractSkillEffect effect;
    public BattleHexUnit target;
    public HexCell cell;
    public int damageDealt;
}


[System.Serializable]
public class SkillEffectZone
{
    public AbstractTargetPattern pattern;           // Например, крест, круг, линия
    public List<AbstractSkillEffect> effects; // Эффекты, применяемые к каждой цели в этой зоне
    
    [Tooltip("Префаб визуального эффекта для этой зоны (показывается на цели после попадания)")]
    public GameObject effectPrefab;
}