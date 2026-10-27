using System.Collections.Generic;
using LARD.Battle;
using UnityEngine;

namespace LARD.Article {
    /// <summary>
    /// 기물을 지정한 위치로 이동시키고 이동 시작 시 진행 중인 스킬을 취소한다.
    /// </summary>
    public sealed class MoveComponentDestination : MoveComponent {
        public readonly struct DestinationReservation {
            public readonly Vector2 Position;
            public readonly float Radius;

            public DestinationReservation(Vector2 position, float radius) {
                Position = position;
                Radius = radius;
            }
        }

        [SerializeField, Min(0f)] private float _arrivalDistance = 0.05f;
        [SerializeField, Min(0f)] private float _avoidanceGap = 0.02f;

        private Vector2 _destination;
        private Vector2 _detourEntry;
        private Vector2 _detourExit;
        private bool _hasDestination;
        private int _detourStage;
        private SkillController _skillController;
        private CapsuleCollider2D _bodyCollider;
        private Map _map;
        private float _bestDistance;
        private float _lastProgressTime;
        private bool _triedReachableDestination;

        public bool IsMoving => _hasDestination;
        public float BodyRadius => GetRadius(_bodyCollider);

        protected override void Awake() {
            base.Awake();
            _skillController = GetComponent<SkillController>();
            _bodyCollider = GetComponent<CapsuleCollider2D>();
            _map = FindFirstObjectByType<Map>();
        }

        public void SetDestination(Vector2 destination) {
            TrySetDestination(destination, null, null, out _);
        }

        public bool TrySetDestination(Vector2 requested, IReadOnlyList<DestinationReservation> reservations,
                                      ICollection<ArticleObj> ignoredArticles, out Vector2 assigned) {
            if (!TryFindFreeDestination(requested, reservations, ignoredArticles, false, out assigned)) {
                Stop();
                return false;
            }

            _destination = assigned;
            if ((_destination - _rigidbody.position).sqrMagnitude <= _arrivalDistance * _arrivalDistance) {
                Stop();
                return true;
            }

            _hasDestination = true;
            _detourStage = 0;
            _bestDistance = Vector2.Distance(_rigidbody.position, _destination);
            _lastProgressTime = Time.time;
            _triedReachableDestination = false;
            _skillController?.CancelCurrentSkill();
            return true;
        }

        public override void Move() {
            if (!_hasDestination) {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 position = _rigidbody.position;
            if ((_destination - position).sqrMagnitude <= _arrivalDistance * _arrivalDistance) {
                Stop();
                return;
            }

            float remaining = Vector2.Distance(position, _destination);
            if (remaining < _bestDistance - Mathf.Max(0.02f, _arrivalDistance)) {
                _bestDistance = remaining;
                _lastProgressTime = Time.time;
            } else if (Time.time - _lastProgressTime > 2f) {
                if (_triedReachableDestination ||
                    !TryFindFreeDestination(_destination, null, null, true, out Vector2 reachable)) {
                    Stop();
                    return;
                }

                _triedReachableDestination = true;
                _destination = reachable;
                _detourStage = 0;
                _bestDistance = Vector2.Distance(position, reachable);
                _lastProgressTime = Time.time;
                if (_bestDistance <= _arrivalDistance) {
                    Stop();
                    return;
                }
            }

            Vector2 target = _detourStage == 1 ? _detourEntry :
                _detourStage == 2 ? _detourExit : _destination;
            if (_detourStage != 0 && (target - position).sqrMagnitude <= _arrivalDistance * _arrivalDistance) {
                _detourStage = _detourStage == 1 ? 2 : 0;
                target = _detourStage == 2 ? _detourExit : _destination;
            }

            if (TryFindBlockingArticle(position, target, out CapsuleCollider2D blocker)) {
                // 우회 중 경로가 잠시 막혀도 매 프레임 반대쪽으로 다시 돌지 않는다.
                if (_detourStage != 0) {
                    _rigidbody.linearVelocity = Vector2.zero;
                    return;
                }
                if (!TryPlanDetour(position, _destination, blocker)) {
                    _rigidbody.linearVelocity = Vector2.zero;
                    return;
                }
                target = _detourEntry;
            }

            Vector2 delta = target - position;
            float distance = delta.magnitude;
            if (distance <= _arrivalDistance) {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            float speed = Mathf.Min(_moveSpeed, distance / Time.fixedDeltaTime);
            _rigidbody.linearVelocity = delta / distance * speed;
        }

        public override void Stop() {
            _hasDestination = false;
            _detourStage = 0;
            _rigidbody.linearVelocity = Vector2.zero;
        }

        private bool TryFindFreeDestination(Vector2 requested,
                                             IReadOnlyList<DestinationReservation> reservations,
                                             ICollection<ArticleObj> ignoredArticles, bool requireClearPath,
                                             out Vector2 destination) {
            if (_map != null) {
                Vector2 halfExtents = _bodyCollider != null ? _bodyCollider.bounds.extents : Vector2.zero;
                requested = _map.ClampArticlePosition(requested, halfExtents);
            }
            destination = requested;
            if (IsInsideArticleArea(requested) &&
                IsDestinationFree(requested, reservations, ignoredArticles) &&
                (!requireClearPath || !TryFindBlockingArticle(_rigidbody.position, requested, out _))) return true;

            Vector2 approach = _rigidbody.position - requested;
            approach = approach.sqrMagnitude > 0.0001f ? approach.normalized : Vector2.right;
            float step = Mathf.Max(0.15f, BodyRadius);
            float limit = _map != null ? _map.ArticleAreaSize * Mathf.Sqrt(2f) : 5f;

            int ringCount = Mathf.CeilToInt(limit / step);
            for (int ring = 1; ring <= ringCount; ring++) {
                float radius = ring * step;
                int angleCount = Mathf.Clamp(Mathf.CeilToInt(2f * Mathf.PI * radius / step), 16, 128);
                for (int angleIndex = 0; angleIndex < angleCount; angleIndex++) {
                    int turn = angleIndex == 0 ? 0 : (angleIndex + 1) / 2 * (angleIndex % 2 == 1 ? 1 : -1);
                    float angle = turn * 2f * Mathf.PI / angleCount;
                    float cosine = Mathf.Cos(angle);
                    float sine = Mathf.Sin(angle);
                    Vector2 direction = new Vector2(approach.x * cosine - approach.y * sine,
                                                    approach.x * sine + approach.y * cosine);
                    Vector2 candidate = requested + direction * radius;
                    if (!IsInsideArticleArea(candidate) ||
                        !IsDestinationFree(candidate, reservations, ignoredArticles) ||
                        (requireClearPath && TryFindBlockingArticle(_rigidbody.position, candidate, out _))) continue;
                    destination = candidate;
                    return true;
                }
            }

            return false;
        }

        private bool IsDestinationFree(Vector2 destination, IReadOnlyList<DestinationReservation> reservations,
                                       ICollection<ArticleObj> ignoredArticles) {
            if (reservations != null) {
                foreach (DestinationReservation reservation in reservations) {
                    float spacing = BodyRadius + reservation.Radius + _avoidanceGap;
                    if ((destination - reservation.Position).sqrMagnitude < spacing * spacing) return false;
                }
            }

            Vector2 center = destination + GetCastOffset();
            float radius = GetRadius(_bodyCollider) + _avoidanceGap * 0.5f;
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, radius)) {
                if (hit is CapsuleCollider2D candidate && candidate != _bodyCollider &&
                    candidate.TryGetComponent<ArticleObj>(out ArticleObj article) &&
                    article.gameObject.activeInHierarchy && !article.IsInPool &&
                    (ignoredArticles == null || !ignoredArticles.Contains(article))) return false;
            }

            return true;
        }

        private bool TryPlanDetour(Vector2 position, Vector2 goal, CapsuleCollider2D blocker) {
            Vector2 forward = (goal - position).normalized;
            if (forward.sqrMagnitude < 0.0001f) return false;

            Vector2 side = new Vector2(-forward.y, forward.x);
            Vector2 center = (Vector2)blocker.bounds.center - GetCastOffset();
            float clearance = GetRadius(_bodyCollider) + GetRadius(blocker) + _avoidanceGap;
            float bestLength = float.PositiveInfinity;
            bool found = false;

            for (int sign = -1; sign <= 1; sign += 2) {
                for (int step = 1; step <= 4; step++) {
                    Vector2 offset = side * (sign * clearance * step);
                    Vector2 entry = center - forward * clearance + offset;
                    Vector2 exit = center + forward * clearance + offset;
                    if (!IsInsideArticleArea(entry) || !IsInsideArticleArea(exit) ||
                        TryFindBlockingArticle(position, entry, out _) ||
                        TryFindBlockingArticle(entry, exit, out _)) {
                        continue;
                    }

                    float length = Vector2.Distance(position, entry) +
                                   Vector2.Distance(entry, exit) + Vector2.Distance(exit, goal);
                    if (length >= bestLength) continue;
                    bestLength = length;
                    _detourEntry = entry;
                    _detourExit = exit;
                    found = true;
                }
            }

            if (found) _detourStage = 1;
            return found;
        }

        private bool TryFindBlockingArticle(Vector2 from, Vector2 to, out CapsuleCollider2D blocker) {
            blocker = null;
            Vector2 delta = to - from;
            float distance = delta.magnitude;
            if (distance <= _arrivalDistance) return false;

            Vector2 castOffset = GetCastOffset();
            float radius = GetRadius(_bodyCollider) + _avoidanceGap * 0.5f;
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit2D hit in Physics2D.CircleCastAll(from + castOffset, radius, delta / distance, distance)) {
                // SelectionCollider는 자식 오브젝트이고 CapsuleCollider2D가 아니다.
                if (hit.collider is not CapsuleCollider2D candidate || candidate == _bodyCollider ||
                    !candidate.TryGetComponent<ArticleObj>(out _)) {
                    continue;
                }

                if (hit.distance <= 0f &&
                    (to + castOffset - (Vector2)candidate.bounds.center).sqrMagnitude >
                    (from + castOffset - (Vector2)candidate.bounds.center).sqrMagnitude) {
                    continue;
                }

                if (hit.distance >= nearest) continue;
                nearest = hit.distance;
                blocker = candidate;
            }

            return blocker != null;
        }

        private bool IsInsideArticleArea(Vector2 point) {
            if (_map == null) return true;
            Vector2 halfExtents = _bodyCollider != null ? _bodyCollider.bounds.extents : Vector2.zero;
            return (point - _map.ClampArticlePosition(point, halfExtents)).sqrMagnitude < 0.000001f;
        }

        private static float GetRadius(CapsuleCollider2D collider) {
            if (collider == null) return 0.15f;
            Vector2 extents = collider.bounds.extents;
            return Mathf.Max(extents.x, extents.y);
        }

        private Vector2 GetCastOffset() {
            return _bodyCollider != null
                ? (Vector2)_bodyCollider.bounds.center - _rigidbody.position
                : Vector2.zero;
        }
    }
}
