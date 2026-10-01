using System.Collections.Generic;
using UnityEngine;

namespace LARD.Skill {
    public static class FindTargetUtil {
        public static List<IAttackTarget> FindTargets(Vector2 center, float range, bool nearestFirst = true) {
            List<IAttackTarget> targets = new();
            if (range <= 0f) {
                return targets;
            }

            HashSet<IAttackTarget> seen = new();
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(center, range)) {
                foreach (MonoBehaviour component in collider.GetComponentsInParent<MonoBehaviour>()) {
                    if (component is not IAttackTarget target ||
                        !target.IsAlive || target.TargetTransform == null || !seen.Add(target)) {
                        continue;
                    }

                    targets.Add(target);
                }
            }

            targets.Sort((left, right) => {
                float leftDistance = ((Vector2)left.TargetTransform.position - center).sqrMagnitude;
                float rightDistance = ((Vector2)right.TargetTransform.position - center).sqrMagnitude;
                return nearestFirst ? leftDistance.CompareTo(rightDistance) : rightDistance.CompareTo(leftDistance);
            });
            return targets;
        }
    }
}
