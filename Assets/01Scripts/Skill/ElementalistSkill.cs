using System.Collections.Generic;
using UnityEngine;

namespace LARD.Skill {
    // Three strikes per cast. Prefer distinct enemies, then repeat living targets.
    public sealed class ElementalistSkill : SkillBase {
        private const int StrikesPerCast = 3;

        public override bool TryUse() {
            if (Owner == null || Owner.Model == null || Model?.Effects == null) return false;

            List<IAttackTarget> candidates = FindTargetUtil.FindTargets(Owner.transform.position, Owner.Model.Range);
            if (candidates.Count == 0) return false;

            float radius = 0f;
            foreach (EffectData data in Model.Effects) {
                if (data != null && data.Effect == SkillEffectType.Damage &&
                    data.Target == EffectTargetType.AreaEnemy &&
                    data.Timing == EffectApplicationTiming.Immediate) {
                    radius = data.Radius;
                    break;
                }
            }
            if (radius <= 0f) return false;

            Vector2 center = candidates[0].TargetTransform.position;
            List<IAttackTarget> targets = FindTargetUtil.FindTargets(center, radius);
            if (targets.Count == 0) return false;

            bool applied = false;
            for (int strike = 0; strike < StrikesPerCast; strike++) {
                IAttackTarget target = null;
                for (int offset = 0; offset < targets.Count; offset++) {
                    IAttackTarget candidate = targets[(strike + offset) % targets.Count];
                    if (!candidate.IsAlive) continue;
                    target = candidate;
                    break;
                }
                if (target == null) break;
                applied |= ApplyToTarget(target, EffectTargetType.AreaEnemy);
            }
            return applied;
        }
    }
}
