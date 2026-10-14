using UnityEngine;

namespace LARD.Skill {
    // One immediate hit to every enemy around the nearest target.
    public sealed class AreaAttackSkill : SkillBase {
        public override bool TryUse() {
            if (Owner == null || Owner.Model == null || Model?.Effects == null) return false;
            var candidates = FindTargetUtil.FindTargets(Owner.transform.position, Owner.Model.Range);
            if (candidates.Count == 0) return false;

            float radius = 0f;
            foreach (EffectData data in Model.Effects) {
                if (data != null && data.Target == EffectTargetType.AreaEnemy &&
                    data.Effect == SkillEffectType.Damage && data.Timing == EffectApplicationTiming.Immediate) {
                    radius = data.Radius;
                    break;
                }
            }
            if (radius <= 0f) return false;

            Vector2 center = candidates[0].TargetTransform.position;
            bool applied = false;
            foreach (IAttackTarget target in FindTargetUtil.FindTargets(center, radius)) {
                applied |= ApplyToTarget(target, EffectTargetType.AreaEnemy);
            }
            return applied;
        }
    }
}
