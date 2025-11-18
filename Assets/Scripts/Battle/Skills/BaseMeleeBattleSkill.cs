using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Battle Skills/Base Melee Attack")]
public class BaseMeleeBattleSkill : AbstractBattleSkill
{
    private void Reset()
    {
        // Инициализируем значения по умолчанию при создании в редакторе
        skillName = "Base Melee Attack";
        staminaCost = 1;
        cooldown = 0;
        targetType = SkillTargetType.EnemyUnit;
        minRange = 1;
        maxRange = 1;
        requiresLineOfSight = false;
        
        // Пользователь должен самостоятельно настроить зоны (zones) через Unity редактор
        // Добавьте нужные паттерны и эффекты через Inspector
    }

    private void OnEnable()
    {
        // Убеждаемся, что базовые значения инициализированы при загрузке
        if (string.IsNullOrEmpty(skillName))
        {
            skillName = "Base Melee Attack";
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

        // Проверяем разницу высот для ближних атак
        // BaseMeleeBattleSkill - это всегда ближние атаки, независимо от maxRange
        // Ближние атаки невозможны, если разница высот >= 2
        int casterElevation = caster.Location.Values.Elevation;
        int targetElevation = targetCell.Values.Elevation;
        int elevationDiff = Mathf.Abs(casterElevation - targetElevation);
        
        if (elevationDiff >= 2)
        {
            return false; // Ближняя атака невозможна при разнице высот >= 2
        }

        // Проверяем тип цели
        HexUnit targetUnit = targetCell.Unit;
        if (targetUnit == null)
            return targetType == SkillTargetType.EmptyCell || targetType == SkillTargetType.AnyCell;

        BattleHexUnit battleTarget = targetUnit as BattleHexUnit;
        if (battleTarget == null)
            return false;

        // Проверяем, является ли цель врагом
        bool isEnemy = !caster.CompareTag(targetUnit.tag);
        
        switch (targetType)
        {
            case SkillTargetType.EnemyUnit:
                return isEnemy;
            case SkillTargetType.AllyUnit:
                return !isEnemy;
            case SkillTargetType.AnyUnit:
                return true;
            default:
                return false;
        }
    }

    public override void ShowTargetingPreview(HexCell targetCell, BattleHexUnit caster)
    {
        // TODO: Реализовать визуализацию предпросмотра атаки
        // Можно подсветить целевую ячейку и затронутые ячейки
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

                    int damageDealt = 0;

                    // Если эффект наносит урон, вычисляем его
                    if (effect is DealDamageEffect && targetUnit != null)
                    {
                        var damageResult = BattleCalculator.CalculateDamage(
                            effect, caster, targetUnit, new BattleContext());
                        damageDealt = damageResult.damageAmount;
                    }

                    // Применяем эффект
                    effect.Apply(caster, targetUnit, cell);

                    // Записываем результат применения
                    result.appliedEffects.Add(new SkillEffectApplication
                    {
                        effect = effect,
                        target = targetUnit,
                        cell = cell,
                        damageDealt = damageDealt
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

