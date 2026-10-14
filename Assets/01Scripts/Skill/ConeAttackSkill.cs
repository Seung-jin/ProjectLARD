using UnityEngine;

namespace LARD.Skill {
    public sealed class ConeAttackSkill : SkillBase {
        public override bool TryUse() {
            if (Owner == null || Owner.Model == null || Model?.Effects == null) return false;

            Vector2 origin = Owner.transform.position;
            var candidates = FindTargetUtil.FindTargets(origin, Owner.Model.Range);
            if (candidates.Count == 0) return false;

            float radius = 0f;
            float angle = 0f;
            foreach (EffectData data in Model.Effects) {
                if (data != null && data.Target == EffectTargetType.AreaEnemy &&
                    data.Effect == SkillEffectType.Damage && data.Timing == EffectApplicationTiming.Immediate) {
                    radius = data.Radius;
                    angle = data.ConeAngleDegrees;
                    break;
                }
            }
            if (radius <= 0f || angle <= 0f) return false;

            Vector2 direction = ((Vector2)candidates[0].TargetTransform.position - origin).normalized;
            float minDot = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);
            bool applied = false;
            foreach (IAttackTarget target in FindTargetUtil.FindTargets(origin, radius)) {
                Vector2 offset = (Vector2)target.TargetTransform.position - origin;
                if (offset.sqrMagnitude > 0f && Vector2.Dot(direction, offset.normalized) < minDot) continue;
                applied |= ApplyToTarget(target, EffectTargetType.AreaEnemy);
            }
            return applied;
        }
    }
}
