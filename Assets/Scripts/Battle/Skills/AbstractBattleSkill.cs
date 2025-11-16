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


    // Основные методы которые нужно реализовать
    public abstract bool IsValidTarget(HexCell targetCell, BattleHexUnit caster);
    public abstract void ShowTargetingPreview(HexCell targetCell, BattleHexUnit caster);
    public abstract void HideTargetingPreview();
    public abstract SkillResult Execute(HexCell targetCell, BattleHexUnit caster);
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
}