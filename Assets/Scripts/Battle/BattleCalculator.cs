using UnityEngine;
using System.Collections.Generic;

public static class BattleCalculator
{
    public static DamageResult CalculateDamage(AbstractSkillEffect effect, BattleHexUnit caster, BattleHexUnit target, BattleContext battleContext)
    {
        int baseAttack = 0;
        int baseDefense = 0;
        int baseDamage = 0;

        // Определяем базовые статы в зависимости от категории ===
        if (effect.category == EffectCategory.Physical)
        {
            baseAttack = caster.PhyAtk;
            baseDefense = target.PhyDef;
        }
        else if (effect.category == EffectCategory.Magical)
        {
            baseAttack = caster.MagAtk;
            baseDefense = target.MagDef;
        }
        else
        {
            // статусные и утилитарные эффекты не зависят от этих статов
            return new DamageResult { damageAmount = 0 };
        }

        // Модификатор силы навыка ===
        baseAttack += effect.power;

        // Базовая формула ===
        baseDamage = Mathf.Max(1, baseAttack - baseDefense);

        // Применяем дополнительные модификаторы ===
        int modifierSum = 0;
        modifierSum += GetElementModifier(effect, caster, target);
        modifierSum += GetHeightModifier(caster, target, effect.category);
        modifierSum += GetSurroundingModifier(caster);
        modifierSum += GetStatusModifiers(caster, target, effect.category);

        baseDamage += modifierSum;

        // Урон не может быть меньше 1
        baseDamage = Mathf.Max(1, baseDamage);

        return new DamageResult
        {
            damageAmount = baseDamage,
            source = caster,
            target = target,
            element = effect.elementType
        };
    }

    // Стихийные преимущества ===
    private static int GetElementModifier(AbstractSkillEffect effect, BattleHexUnit caster, BattleHexUnit target)
    {
        if (effect.category != EffectCategory.Magical)
        {
            return 0; // Стихийность влияет только на магию
        }

        switch (effect.elementType)
        {
            case ElementType.Fire:
                if (target.elementType == ElementType.Grass) return +2;
                break;
            case ElementType.Water:
                if (target.elementType == ElementType.Fire) return +2;
                break;
            case ElementType.Grass:
                if (target.elementType == ElementType.Water) return +2;
                break;
            default:
                return 0;
        }

        return 0;
    }

    // Позиционные модификаторы (рельеф) ===
    private static int GetHeightModifier(BattleHexUnit caster, BattleHexUnit target, EffectCategory category)
    {
        // Магические атаки не зависят от рельефа
        if (category != EffectCategory.Physical)
        {
            return 0;
        }

        // Получаем клетки кастера и цели
        HexCell casterCell = caster.Location;
        HexCell targetCell = target.Location;

        // Получаем высоты клеток
        int casterElevation = casterCell.Values.Elevation;
        int targetElevation = targetCell.Values.Elevation;

        // Вычисляем разницу высот
        int elevationDiff = casterElevation - targetElevation;

        // Модификаторы для физических ближних атак:
        // +1 к атаке сверху вниз (кастер выше цели)
        // -1 к атаке снизу вверх (кастер ниже цели)
        if (elevationDiff > 0)
        {
            return +1; // Атака сверху вниз
        }
        else if (elevationDiff < 0)
        {
            return -1; // Атака снизу вверх
        }

        return 0; // На одной высоте
    }

    // Окружение ===
    private static int GetSurroundingModifier(BattleHexUnit caster)
    {
        return 0; // Реализовать окружение
        // var nearbyEnemies = BattleGrid.Instance.GetAdjacentEnemies(caster);
        // return nearbyEnemies.Count >= 3 ? +1 : 0;
    }

    // Статусы ===
    private static int GetStatusModifiers(BattleHexUnit caster, BattleHexUnit target, EffectCategory category)
    {
        int mod = 0;

        // Усиление
        if (caster.HasStatus(StatusType.Empowered))
        {
            if (category == EffectCategory.Physical) mod += 1;
            if (category == EffectCategory.Magical) mod += 1;
        }

        // Горение
        if (caster.HasStatus(StatusType.Burning))
            mod -= 1; // -1 к MagAtk
        // Отравление
        if (caster.HasStatus(StatusType.Poisoned))
            mod -= 1; // -1 к PhyAtk
        // Хрупкость у цели
        if (target.HasStatus(StatusType.Fragile))
            mod += 1; // +1 к получаемому урону

        return mod;
    }

    /// <summary>
    /// Вычисляет количество лечения на основе эффекта, кастера и цели
    /// Формула: max(1, power + модификаторы)
    /// Без учета атаки кастера и защиты цели
    /// </summary>
    public static HealingResult CalculateHealing(AbstractSkillEffect effect, BattleHexUnit caster, BattleHexUnit target, BattleContext battleContext)
    {
        // Базовая формула лечения: только power (без атаки кастера)
        int baseHealing = effect.power;

        // Применяем дополнительные модификаторы
        int modifierSum = 0;
        modifierSum += GetHealingElementModifier(effect, caster, target);
        modifierSum += GetHealingStatusModifiers(caster, target, effect.category);

        baseHealing += modifierSum;

        // Лечение не может быть меньше 1
        baseHealing = Mathf.Max(1, baseHealing);

        return new HealingResult
        {
            healingAmount = baseHealing,
            source = caster,
            target = target,
            element = effect.elementType
        };
    }

    /// <summary>
    /// Стихийные модификаторы для лечения (противоположны модификаторам атаки)
    /// Если стихия эффективна против цели в атаке, то она неэффективна для лечения
    /// </summary>
    private static int GetHealingElementModifier(AbstractSkillEffect effect, BattleHexUnit caster, BattleHexUnit target)
    {
        // Стихийность влияет только на магическое лечение
        if (effect.category != EffectCategory.Magical)
        {
            return 0;
        }

        // Модификаторы лечения противоположны модификаторам атаки
        // Если стихия эффективна против цели в атаке (+2), то она неэффективна для лечения (-2)
        // Если стихия слаба против цели в атаке (0), то она эффективна для лечения (+2)
        switch (effect.elementType)
        {
            case ElementType.Fire:
                // Огонь эффективен против Травы в атаке → неэффективен для лечения Травы
                if (target.elementType == ElementType.Grass) return -2;
                // Огонь слаб против Воды в атаке → эффективен для лечения Воды
                if (target.elementType == ElementType.Water) return +2;
                break;
            case ElementType.Water:
                // Вода эффективна против Огня в атаке → неэффективна для лечения Огня
                if (target.elementType == ElementType.Fire) return -2;
                // Вода слаба против Травы в атаке → эффективна для лечения Травы
                if (target.elementType == ElementType.Grass) return +2;
                break;
            case ElementType.Grass:
                // Трава эффективна против Воды в атаке → неэффективна для лечения Воды
                if (target.elementType == ElementType.Water) return -2;
                // Трава слаба против Огня в атаке → эффективна для лечения Огня
                if (target.elementType == ElementType.Fire) return +2;
                break;
            default:
                return 0;
        }

        return 0;
    }

    /// <summary>
    /// Модификаторы лечения от статусов
    /// </summary>
    private static int GetHealingStatusModifiers(BattleHexUnit caster, BattleHexUnit target, EffectCategory category)
    {
        int mod = 0;

        // Усиление увеличивает лечение
        if (caster.HasStatus(StatusType.Empowered))
        {
            if (category == EffectCategory.Physical) mod += 1;
            if (category == EffectCategory.Magical) mod += 1;
        }

        // Горение уменьшает магическое лечение
        if (caster.HasStatus(StatusType.Burning) && category == EffectCategory.Magical)
            mod -= 1;

        // Отравление уменьшает физическое лечение
        if (caster.HasStatus(StatusType.Poisoned) && category == EffectCategory.Physical)
            mod -= 1;

        // TODO: Можно добавить статусы, которые увеличивают получаемое лечение у цели

        return mod;
    }
}

public struct DamageResult
{
    public int damageAmount;
    public BattleHexUnit source;
    public BattleHexUnit target;
    public ElementType element;
}

public struct HealingResult
{
    public int healingAmount;
    public BattleHexUnit source;
    public BattleHexUnit target;
    public ElementType element;
}

public struct BattleContext
{
    // Добавить BattleContext (структуру), если захочешь учитывать бафы от союзников, погоду, тип местности
}