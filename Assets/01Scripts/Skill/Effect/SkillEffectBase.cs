using LARD.Article;
using LARD.Battle;
using LARD.Buff;
using LARD.Monster;

namespace LARD.Skill.Effect {
    /// <summary>
    /// EffectData의 효과 종류별 실제 적용 동작을 정의하는 기본 클래스다.
    /// </summary>
    public abstract class SkillEffectBase {
        /// <summary>시전자와 설정값을 사용해 대상에 이펙트를 적용한다.</summary>
        public abstract void Apply(ArticleObj attacker, EffectData data, object target);
    }

    public sealed class DamageSkillEffect : SkillEffectBase {
        public override void Apply(ArticleObj attacker, EffectData data, object target) {
            if (target is IAttackTarget enemy)
                DamageLogic.ApplyDamage(attacker, data.Damage, enemy);
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
            if (attacker != null && attacker.Id > 0 && target is ArticleObj ally && ally.Model != null)
                ally.BuffController.AddOrRefresh(BuffType.SkillDamageIncrease, attacker.Id,
                                                 data.ValuePercent, data.DurationSeconds);
        }
    }

    public sealed class AttackSpeedIncreaseSkillEffect : SkillEffectBase {
        public override void Apply(ArticleObj attacker, EffectData data, object target) {
            if (attacker != null && attacker.Id > 0 && target is ArticleObj ally && ally.Model != null)
                ally.BuffController.AddOrRefresh(BuffType.AttackSpeedIncrease, attacker.Id,
                                                 data.ValuePercent, data.DurationSeconds);
        }
    }
}
