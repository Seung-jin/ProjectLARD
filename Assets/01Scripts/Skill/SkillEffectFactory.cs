using LARD.Skill.Effect;

namespace LARD.Skill {
    public static class SkillEffectFactory {
        private static readonly SkillEffectBase DamageEffect = new DamageSkillEffect();

        public static SkillEffectBase Create(SkillEffectType type) {
            return type switch {
                SkillEffectType.Damage => DamageEffect,
                _ => null,
            };
        }
    }
}
