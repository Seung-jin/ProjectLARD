using LAD.Battle;
using LAD.Stat;
using UnityEngine;

namespace LAD.Skill {
    public enum DamageArea {
        SingleTarget,
        TargetCircle,
        SelfCircle,
        ForwardBox,
    }

    public abstract class BasicDamageSkill : SkillBase {
        private readonly DamageArea area;
        private readonly StatType scalingStat;
        private readonly DamageType damageType;
        private readonly bool notifySkillHit;

        protected BasicDamageSkill(DamageArea area, StatType scalingStat, DamageType damageType, bool notifySkillHit) {
            this.area = area;
            this.scalingStat = scalingStat;
            this.damageType = damageType;
            this.notifySkillHit = notifySkillHit;
        }

        public override bool Activate() {
            if (!TryGetCurrentTarget(out IDamageable target) || target is not Component component) return false;
            float damage = CalculateScaledDamage(scalingStat);
            if (damage <= 0f) return false;

            return area switch {
                DamageArea.SingleTarget => SkillUtility.DamageSingle(
                    Owner, target, damage, damageType, notifySkillHit),
                DamageArea.TargetCircle => TryRadius(out float targetRadius) && SkillUtility.DamageCircle(
                    Owner, component.transform.position, targetRadius, damage, damageType, notifySkillHit) > 0,
                DamageArea.SelfCircle => TryRadius(out float selfRadius) && SkillUtility.DamageCircle(
                    Owner, Owner.transform.position, selfRadius, damage, damageType, notifySkillHit) > 0,
                DamageArea.ForwardBox => TryArea(out float length, out float width) &&
                                         SkillUtility.DamageForwardBox(
                                             Owner, SkillUtility.DirectionTo(Owner, component),
                                             length, width, damage, damageType, notifySkillHit) > 0,
                _ => false,
            };
        }

        public override float GetBaseDamage() => CalculateScaledDamage(scalingStat);

        private bool TryRadius(out float radius) {
            radius = 0f;
            return GradeData != null && GradeData.TryGetValue(SkillValueKeys.EffectRadius, out radius) &&
                   (radius = Mathf.Max(0f, radius)) > 0f;
        }

        private bool TryArea(out float length, out float width) {
            length = 0f;
            width = 0f;
            return GradeData != null &&
                   GradeData.TryGetValue(SkillValueKeys.EffectRadius, out length) &&
                   GradeData.TryGetValue(SkillValueKeys.EffectWidth, out width) &&
                   (length = Mathf.Max(0f, length)) > 0f &&
                   (width = Mathf.Max(0f, width)) > 0f;
        }
    }
}
