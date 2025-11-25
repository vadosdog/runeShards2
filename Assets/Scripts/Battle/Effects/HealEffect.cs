using UnityEngine;

[CreateAssetMenu(menuName = "SkillEffects/Heal")]
public class HealEffect : AbstractSkillEffect
{
    public override void Apply(BattleHexUnit caster, BattleHexUnit target, HexCell targetCell)
    {
        if (target == null) return;

        var healResult = BattleCalculator.CalculateHealing(this, caster, target, new BattleContext());
        target.Heal(healResult.healingAmount);
    }
}

