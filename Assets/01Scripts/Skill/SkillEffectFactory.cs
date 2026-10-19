using LARD.Skill.Effect;

namespace LARD.Skill {
    /// <summary>
    /// 이펙트 종류를 실제 적용 로직에 연결하고 재사용 가능한 구현체를 제공한다.
    /// </summary>
    public static class SkillEffectFactory {
        private static readonly SkillEffectBase DamageEffect = new DamageSkillEffect();
        private static readonly SkillEffectBase DamageTakenIncreaseEffect = new DamageTakenIncreaseSkillEffect();
        private static readonly SkillEffectBase MoveSpeedDecreaseEffect = new MoveSpeedDecreaseSkillEffect();
        private static readonly SkillEffectBase DefenseDecreaseEffect = new DefenseDecreaseSkillEffect();
        private static readonly SkillEffectBase SkillDamageIncreaseEffect = new SkillDamageIncreaseSkillEffect();
        private static readonly SkillEffectBase AttackSpeedIncreaseEffect = new AttackSpeedIncreaseSkillEffect();

        /// <summary>효과 종류에 해당하는 구현체를 반환하며, 지원하지 않는 종류는 null을 반환한다.</summary>
        public static SkillEffectBase Create(SkillEffectType type) {
            return type switch {
                SkillEffectType.Damage => DamageEffect,
                SkillEffectType.DamageTakenIncrease => DamageTakenIncreaseEffect,
                SkillEffectType.MoveSpeedDecrease => MoveSpeedDecreaseEffect,
                SkillEffectType.DefenseDecrease => DefenseDecreaseEffect,
                SkillEffectType.SkillDamageIncrease => SkillDamageIncreaseEffect,
                SkillEffectType.AttackSpeedIncrease => AttackSpeedIncreaseEffect,
                _ => null,
            };
        }
    }
}
