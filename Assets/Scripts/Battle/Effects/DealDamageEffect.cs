using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "SkillEffects/DealDamage")]
public class DealDamageEffect : AbstractSkillEffect
{
    public override void Apply(BattleHexUnit caster, BattleHexUnit target, HexCell targetCell)
    {
        if (target == null) return;

        var damageResult = BattleCalculator.CalculateDamage(this, caster, target, new BattleContext());
        target.TakeDamage(damageResult.damageAmount);
    }
}