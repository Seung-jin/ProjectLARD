using LAD.Battle;
using UnityEngine;

namespace LAD.Skill.Effect {
    public sealed class DamageSkillEffect : SkillEffectBase {
        public override bool Activate(EffectData data, BattleObj source, BattleObj target) {
            if (target == null) {
                Debug.LogWarning("[Effect:Damage] 대미지를 적용할 수 있는 대상이 아닙니다.", target);
                return false;
            }

            float appliedDamage = BattleLogic.ApplyDamage(data.Value, source, target, data.DamageType, "스킬 효과", data.HasPeriodicExecution ? AttackCategory.Periodic : AttackCategory.Skill);
            Debug.Log(
                $"[Effect:Damage] 적용 완료: Raw={data.Value:0.##}, " +
                $"Applied={appliedDamage:0.##}, DamageType={data.DamageType}",
                target);
            return appliedDamage > 0f;
        }
    }
}
