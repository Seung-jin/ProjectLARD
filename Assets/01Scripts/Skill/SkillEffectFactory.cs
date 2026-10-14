using LARD.Skill.Effect;

namespace LARD.Skill {
    public static class SkillEffectFactory {
        private static readonly SkillEffectBase DamageEffect = new DamageSkillEffect();
        private static readonly SkillEffectBase DamageTakenIncreaseEffect = new DamageTakenIncreaseSkillEffect();
        private static readonly SkillEffectBase MoveSpeedDecreaseEffect = new MoveSpeedDecreaseSkillEffect();
        private static readonly SkillEffectBase DefenseDecreaseEffect = new DefenseDecreaseSkillEffect();
        private static readonly SkillEffectBase SkillDamageIncreaseEffect = new SkillDamageIncreaseSkillEffect();

        public static SkillEffectBase Create(SkillEffectType type) {
            return type switch {
                SkillEffectType.Damage => DamageEffect,
                SkillEffectType.DamageTakenIncrease => DamageTakenIncreaseEffect,
                SkillEffectType.MoveSpeedDecrease => MoveSpeedDecreaseEffect,
                SkillEffectType.DefenseDecrease => DefenseDecreaseEffect,
                SkillEffectType.SkillDamageIncrease => SkillDamageIncreaseEffect,
                _ => null,
            };
        }
    }
}
