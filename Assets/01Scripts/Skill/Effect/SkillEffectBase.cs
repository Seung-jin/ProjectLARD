using LARD.Article;
using LARD.Battle;
using LARD.Monster;

namespace LARD.Skill.Effect {
    public abstract class SkillEffectBase {
        public abstract void Apply(ArticleObj attacker, EffectData data, IAttackTarget target);
    }

    public sealed class DamageSkillEffect : SkillEffectBase {
        public override void Apply(ArticleObj attacker, EffectData data, IAttackTarget target) {
            DamageLogic.ApplyDamage(attacker, data.AttackPowerRatio, target);
        }
    }

    public sealed class DamageTakenIncreaseSkillEffect : SkillEffectBase {
        public override void Apply(ArticleObj attacker, EffectData data, IAttackTarget target) {
            if (target is MonsterObj monster && monster.IsAlive) {
                monster.EffectController.ApplyDamageTakenIncrease(data.ValuePercent, data.DurationSeconds);
            }
        }
    }

    public sealed class MoveSpeedDecreaseSkillEffect : SkillEffectBase {
        public override void Apply(ArticleObj attacker, EffectData data, IAttackTarget target) {
            if (target is MonsterObj monster && monster.IsAlive) {
                monster.EffectController.ApplyMoveSpeedDecrease(data.ValuePercent, data.DurationSeconds);
            }
        }
    }
}
