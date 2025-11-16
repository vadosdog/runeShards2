using UnityEngine;
using System.Collections.Generic;

public abstract class AbstractSkillEffect : ScriptableObject
{
    public ElementType elementType;   // Стихия: Огонь, Вода, Земля...
    public EffectCategory category;   // Физический, Магический, Статусный и т.д.
    public int power;               // Сила эффекта

    public abstract void Apply(BattleHexUnit caster, BattleHexUnit target, HexCell targetCell);
}