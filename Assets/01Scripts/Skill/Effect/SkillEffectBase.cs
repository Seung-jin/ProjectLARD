using LARD.Article;
using LARD.Battle;
using LARD.Monster;

namespace LARD.Skill.Effect {
    public abstract class SkillEffectBase {
        public abstract void Apply(ArticleObj attacker, EffectData data, object target);
    }

    public sealed class DamageSkillEffect : SkillEffectBase {
        public override void Apply(ArticleObj attacker, EffectData data, object target) {
            if (target is IAttackTarget enemy && attacker.SkillController?.Model != null)
                DamageLogic.ApplyDamage(attacker, attacker.SkillController.Model.Damage, enemy);
        }
    }

    public sealed class DamageTakenIncreaseSkillEffect : SkillEffectBase {
        public override void Apply(ArticleObj attacker, EffectData data, object target) {
            if (target is MonsterObj monster && monster.IsAlive) {
                monster.EffectController.ApplyDamageTakenIncrease(data.ValuePercent, data.DurationSeconds);
            }
        }
    }

    public sealed class MoveSpeedDecreaseSkillEffect : SkillEffectBase {
        public override void Apply(ArticleObj attacker, EffectData data, object target) {
            if (target is MonsterObj monster && monster.IsAlive) {
                monster.EffectController.ApplyMoveSpeedDecrease(data.ValuePercent, data.DurationSeconds);
            }
        }
    }

    public sealed class DefenseDecreaseSkillEffect : SkillEffectBase {
        public override void Apply(ArticleObj attacker, EffectData data, object target) {
            if (target is MonsterObj monster && monster.IsAlive)
                monster.EffectController.ApplyDefenseDecrease(data.ValuePercent, data.DurationSeconds);
        }
    }

    public sealed class SkillDamageIncreaseSkillEffect : SkillEffectBase {
        public override void Apply(ArticleObj attacker, EffectData data, object target) {
            if (target is ArticleObj ally && ally.Model != null)
                ally.EffectController.ApplySkillDamageIncrease(data.ValuePercent, data.DurationSeconds);
        }
    }
}
