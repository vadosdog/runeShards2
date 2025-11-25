using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Battle Skills/Base Heal")]
public class BaseHealBattleSkill : AbstractBattleSkill
{
    private void Reset()
    {
        // Инициализируем значения по умолчанию при создании в редакторе
        skillName = "Base Heal";
        staminaCost = 2;
        cooldown = 0;
        targetType = SkillTargetType.AllyUnit; // Лечение работает на союзников
        minRange = 0; // Можно лечить на себя (дистанция 0)
        maxRange = 3; // По умолчанию дальность 3
        requiresLineOfSight = false; // Лечение не требует прямой видимости
        
        // Пользователь должен самостоятельно настроить зоны (zones) через Unity редактор
        // Добавьте нужные паттерны и эффекты через Inspector
    }

    private void OnEnable()
    {
        // Убеждаемся, что базовые значения инициализированы при загрузке
        if (string.IsNullOrEmpty(skillName))
        {
            skillName = "Base Heal";
        }
    }

    public override bool IsValidTarget(HexCell targetCell, BattleHexUnit caster)
    {
        if (targetCell == null || caster == null)
            return false;

        // Проверяем расстояние
        int distance = caster.Location.Coordinates.DistanceTo(targetCell.Coordinates);
        if (distance < minRange || distance > maxRange)
            return false;

        // Лечение не зависит от врагов поблизости (в отличие от дальних атак)
        // Не проверяем HasAdjacentEnemyWithLowHeightDiff

        // Проверяем тип цели
        HexUnit targetUnit = targetCell.Unit;
        if (targetUnit == null)
        {
            // Лечение требует наличия юнита на ячейке
            return false;
        }

        BattleHexUnit battleTarget = targetUnit as BattleHexUnit;
        if (battleTarget == null)
            return false;

        // Проверяем, является ли цель союзником
        bool isEnemy = !caster.CompareTag(targetUnit.tag);
        
        switch (targetType)
        {
            case SkillTargetType.AllyUnit:
                return !isEnemy; // Лечение работает только на союзников
            case SkillTargetType.AnyUnit:
                return true; // Можно лечить любую цель
            case SkillTargetType.EnemyUnit:
                // Обычно лечение не работает на врагов, но если нужно - можно разрешить
                return isEnemy;
            default:
                return false;
        }
    }

    public override void ShowTargetingPreview(HexCell targetCell, BattleHexUnit caster)
    {
        // TODO: Реализовать визуализацию предпросмотра лечения
        // Можно подсветить целевую ячейку зеленым цветом
    }

    public override void HideTargetingPreview()
    {
        // TODO: Скрыть визуализацию предпросмотра
    }

    public override SkillResult Execute(HexCell targetCell, BattleHexUnit caster)
    {
        SkillResult result = new SkillResult
        {
            success = false,
            appliedEffects = new List<SkillEffectApplication>()
        };

        if (!IsValidTarget(targetCell, caster))
        {
            Debug.LogWarning($"Недопустимая цель для навыка {skillName}");
            return result;
        }

        // Проверяем стоимость стамины
        if (caster.currentStamina < staminaCost)
        {
            Debug.LogWarning($"Недостаточно стамины для использования {skillName}");
            return result;
        }

        // Проверяем наличие зон
        if (zones == null || zones.Count == 0)
        {
            Debug.LogWarning($"У навыка {skillName} нет настроенных зон");
            return result;
        }

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

                    int healingAmount = 0;

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
                        damageDealt = -healingAmount // Отрицательное значение для лечения (для совместимости)
                    });
                }
            }
        }

        // Тратим стамину
        caster.ConsumeStamina(staminaCost);

        result.success = true;
        Debug.Log($"{caster.name} использовал {skillName} на {targetCell.Coordinates}");
        
        return result;
    }
}

