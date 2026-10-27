using System.Collections.Generic;
using UnityEngine;

namespace LARD.Skill {
	/// <summary>범위 내 공격 대상 검색과 거리순 정렬.</summary>
	public static class FindTargetUtil {
		public static List<IAttackTarget> FindTargets(Vector2 center, float range, bool nearestFirst = true) {
			List<IAttackTarget> targets = new();
			if (range <= 0f) {
				Debug.Log($"[FindTargetUtil] 탐색된 적: 0마리 (중심: {center}, 범위: {range})");
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
