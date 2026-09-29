using LAD.Battle;
using LAD.Buff;
using LAD.Stat;

namespace LAD.Skill.Effect {
    public sealed class DamageTakenIncreaseSkillEffect : SkillEffectBase {
        public override bool Activate(EffectData data, BattleObj source, BattleObj target) {
            if (target == null || target.IsDead || target.BuffController == null || data.Duration <= 0f) return false;
            string id = $"SkillEffect.{data.StackKey}";
            if (data.StackScope == EffectStackScope.PerSource && source != null)
                id += $".Source{source.GetEntityId()}";
            target.BuffController.AddBuff(new LAD.Buff.Buff(id, BuffType.Debuff, data.Duration,
                new[] { new StatModifier(StatType.DamageTakenIncrease, data.Value) }),
                data.StackPolicy, data.DurationPolicy, data.MaxStacks);
            return true;
        }
    }
}
