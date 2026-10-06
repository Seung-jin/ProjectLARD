using LARD.Article;
using LARD.Battle;

namespace LARD.Skill.Effect {
    public abstract class SkillEffectBase {
        public abstract void Apply(ArticleObj attacker, EffectData data, IAttackTarget target);
    }

    public sealed class DamageSkillEffect : SkillEffectBase {
        public override void Apply(ArticleObj attacker, EffectData data, IAttackTarget target) {
            DamageLogic.ApplyDamage(attacker, data.AttackPowerRatio, target);
        }
    }
}
