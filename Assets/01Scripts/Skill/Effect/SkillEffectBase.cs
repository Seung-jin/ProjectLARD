namespace LARD.Skill.Effect {
    public abstract class SkillEffectBase {
        public abstract void Apply(EffectData data, IAttackTarget target);
    }

    public sealed class DamageSkillEffect : SkillEffectBase {
        public override void Apply(EffectData data, IAttackTarget target) {
            target.TakeDamage(data.Damage);
        }
    }
}
