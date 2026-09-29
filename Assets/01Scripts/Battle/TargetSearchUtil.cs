using System.Collections.Generic;
using UnityEngine;
using BattleMap = LAD.Map.Map;

namespace LAD.Battle {
    public static class TargetSearchUtil {
		public static bool TryGetNearestMonsterPathPosition(
			BattleMap map,
			Vector2 origin,
			out Vector2 pathPosition) {
			return TryGetNearestMonsterPathPosition(map, origin, 0, out pathPosition);
		}

		/// <summary>현재 위치와 가장 가까운 경로 좌표와 그 선분의 진행 방향을 반환합니다.</summary>
		public static bool TryGetNearestMonsterPathPose(
			BattleMap map, Vector2 origin, out Vector2 pathPosition, out Vector2 pathDirection) {
			pathPosition = default;
			pathDirection = Vector2.right;
			if (map == null) return false;

			Transform previous = map.MonsterSpawnPoint;
			bool found = false;
			float nearestDistance = float.PositiveInfinity;
			IReadOnlyList<Transform> path = map.MonsterPath;
			for (int index = 0; index < path.Count; index++) {
				Transform current = path[index];
				if (current == null) continue;
				if (previous == null) {
					previous = current;
					continue;
				}

				Vector2 start = previous.position;
				Vector2 end = current.position;
				Vector2 candidate = GetNearestPointOnSegment(origin, start, end);
				float distance = (candidate - origin).sqrMagnitude;
				if (distance < nearestDistance) {
					pathPosition = candidate;
					Vector2 direction = end - start;
					pathDirection = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
					nearestDistance = distance;
					found = true;
				}
				previous = current;
			}

			if (!found && previous != null) {
				pathPosition = previous.position;
				found = true;
			}
			return found;
		}

		/// <summary>몬스터를 경로의 결승점에 가까운 순서로 정렬합니다.</summary>
		public static void SortByMonsterPathGoal(List<BattleObj> targets, BattleMap map) {
			if (targets == null || targets.Count < 2 || map == null) return;
			Vector2 goal = map.MonsterSpawnPosition;
			for (int index = map.MonsterPath.Count - 1; index >= 0; index--) {
				if (map.MonsterPath[index] == null) continue;
				goal = map.MonsterPath[index].position;
				break;
			}
			targets.Sort((left, right) => {
				int leftIndex = left != null && left.MoveComponent is LAD.Monster.MonsterMoveComponent leftMove
					? leftMove.CurrentPathIndex : -1;
				int rightIndex = right != null && right.MoveComponent is LAD.Monster.MonsterMoveComponent rightMove
					? rightMove.CurrentPathIndex : -1;
				int progress = rightIndex.CompareTo(leftIndex);
				if (progress != 0) return progress;
				return (((Vector2)left.transform.position - goal).sqrMagnitude)
					.CompareTo(((Vector2)right.transform.position - goal).sqrMagnitude);
			});
		}

		/// <summary>거리순 경로 후보 중 지정한 순서의 위치를 반환합니다. 후보가 부족하면 첫 위치를 반환합니다.</summary>
		public static bool TryGetNearestMonsterPathPosition(
			BattleMap map,
			Vector2 origin,
			int candidateIndex,
			out Vector2 pathPosition) {
			pathPosition = default;
			if (map == null) {
				return false;
			}

			Transform previousPoint = map.MonsterSpawnPoint;
			var candidates = new List<Vector2>();

			if (previousPoint != null) {
				candidates.Add(previousPoint.position);
			}

            IReadOnlyList<Transform> path = map.MonsterPath;
            for (int index = 0; index < path.Count; index++) {
                Transform currentPoint = path[index];
                if (currentPoint == null) {
                    continue;
                }

                Vector2 candidate = previousPoint != null
                    ? GetNearestPointOnSegment(origin, previousPoint.position, currentPoint.position)
                    : (Vector2)currentPoint.position;
				candidates.Add(candidate);

                previousPoint = currentPoint;
            }

			if (candidates.Count == 0) {
				return false;
			}

			candidates.Sort((left, right) =>
				((left - origin).sqrMagnitude).CompareTo((right - origin).sqrMagnitude));
			pathPosition = candidates[Mathf.Clamp(candidateIndex, 0, candidates.Count - 1)];
			return true;
        }

		public static bool TryGetNonOverlappingMonsterPathPosition(
			BattleMap map, Vector2 origin, float radius, int candidateIndex, out Vector2 pathPosition) {
			pathPosition = default;
			if (map == null) return false;
			var candidates = new List<Vector2>();
			Transform previous = map.MonsterSpawnPoint;
			if (previous != null) candidates.Add(previous.position);
			IReadOnlyList<Transform> path = map.MonsterPath;
			for (int i = 0; i < path.Count; i++) {
				Transform current = path[i];
				if (current == null) continue;
				candidates.Add(previous != null
					? GetNearestPointOnSegment(origin, previous.position, current.position)
					: (Vector2)current.position);
				previous = current;
			}
			if (candidates.Count == 0) return false;
			int centerIndex = 0;
			float centerDistance = float.MaxValue;
			for (int i = 0; i < candidates.Count; i++) {
				float distance = (candidates[i] - origin).sqrMagnitude;
				if (distance < centerDistance) { centerDistance = distance; centerIndex = i; }
			}

			// 기준점 이후에는 경로 순서상 앞/뒤를 번갈아 선택합니다.
			Vector2 center = candidates[centerIndex];
			if (candidateIndex == 0) {
				pathPosition = center;
				return true;
			}

			// 경로의 실제 끝점을 선택하면 두 번째 영역이 과도하게 멀어질 수 있으므로,
			// 첫 발동 지점을 기준으로 콜라이더 지름만큼 직접 오프셋합니다.
			float spacing = Mathf.Max(0f, radius) * 2f + 0.1f;
			Vector2 pathStart = candidates[0];
			Vector2 pathEnd = candidates[candidates.Count - 1];
			Vector2 pathDirection = pathEnd - pathStart;
			if (pathDirection.sqrMagnitude <= Mathf.Epsilon) pathDirection = center - origin;
			if (pathDirection.sqrMagnitude <= Mathf.Epsilon) pathDirection = Vector2.right;
			pathDirection.Normalize();
			int offset = (candidateIndex + 1) / 2;
			float side = candidateIndex % 2 == 1 ? 1f : -1f;
			pathPosition = center + pathDirection * (side * spacing * offset);
			return true;
		}

        public static List<BattleObj> Search(Vector2 origin, float range, IReadOnlyList<string> targetTags,
                                                      TargetSortMode sortMode = TargetSortMode.None, BattleObj excludedTarget = null) {
            range = Mathf.Max(0f, range);
            Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, range);
            List<BattleObj> targets = new List<BattleObj>();

            for (int i = 0; i < colliders.Length; i++) {
                BattleObj target = colliders[i].GetComponentInParent<BattleObj>();
                if (target == null || target == excludedTarget || targets.Contains(target)) {
                    continue;
                }

                // 콜라이더가 자식 오브젝트에 붙어 있는 경우 자식에만 태그가 설정될 수
                // 있으므로 루트와 실제 콜라이더 양쪽의 태그를 확인합니다.
                if (!HasTargetTag(target.gameObject, targetTags) &&
                    !HasTargetTag(colliders[i].gameObject, targetTags)) {
                    continue;
                }

                targets.Add(target);
            }

            SortTargets(targets, origin, sortMode);
            return targets;
        }

        private static Vector2 GetNearestPointOnSegment(Vector2 point, Vector2 start, Vector2 end) {
            Vector2 segment = end - start;
            float sqrLength = segment.sqrMagnitude;
            if (sqrLength <= Mathf.Epsilon) {
                return start;
            }

            float distanceAlongSegment = Vector2.Dot(point - start, segment) / sqrLength;
            return start + segment * Mathf.Clamp01(distanceAlongSegment);
        }

        private static bool HasTargetTag(GameObject target, IReadOnlyList<string> targetTags) {
            if (targetTags == null || targetTags.Count == 0) {
                return false;
            }

            string targetTag = target.tag;
            for (int i = 0; i < targetTags.Count; i++) {
                if (!string.IsNullOrEmpty(targetTags[i]) && targetTag == targetTags[i]) {
                    return true;
                }
            }

            return false;
        }

        private static void SortTargets(
            List<BattleObj> targets,
            Vector2 origin,
            TargetSortMode sortMode) {
            if (sortMode == TargetSortMode.None || targets.Count < 2) {
                return;
            }

            targets.Sort((left, right) => {
                float leftDistance = ((Vector2)left.transform.position - origin).sqrMagnitude;
                float rightDistance = ((Vector2)right.transform.position - origin).sqrMagnitude;
                int comparison = leftDistance.CompareTo(rightDistance);

                return sortMode == TargetSortMode.Nearest ? comparison : -comparison;
            });
        }
    }
}
