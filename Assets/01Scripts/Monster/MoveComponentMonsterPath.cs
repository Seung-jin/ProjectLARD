using LARD.Battle;
using UnityEngine;

namespace LARD.Monster {
    public sealed class MoveComponentMonsterPath : MoveComponent {
        [SerializeField, Min(0f)] private float _arrivalDistance = 0.05f;

        private Map _map;
        private int _nextWaypointIndex;
        private MonsterObj _monster;

        protected override void Awake() {
            base.Awake();
            _monster = GetComponent<MonsterObj>();
        }

        public void SetPath(Map map) {
            _map = map;
            _nextWaypointIndex = 1;
            Stop();
        }

        public bool TryGetStraightDirection(float requiredDistance, out Vector2 direction, out float pathWidth) {
            direction = Vector2.zero;
            pathWidth = 0f;
            if (_map == null || requiredDistance <= 0f) {
                return false;
            }

            Vector2 start = _map.GetMonsterWaypoint(_nextWaypointIndex - 1);
            Vector2 end = _map.GetMonsterWaypoint(_nextWaypointIndex);
            Vector2 segment = end - start;
            if (segment.sqrMagnitude <= 0f) {
                return false;
            }

            direction = segment.normalized;
            pathWidth = _map.MonsterPathWidth;
            float remainingDistance = Vector2.Dot(end - (Vector2)transform.position, direction);
            return remainingDistance >= requiredDistance;
        }

        public override void Move() {
            if (_map == null) {
                Stop();
                return;
            }

            Vector2 target = _map.GetMonsterWaypoint(_nextWaypointIndex);
            Vector2 delta = target - _rigidbody.position;
            if (delta.sqrMagnitude <= _arrivalDistance * _arrivalDistance) {
                _nextWaypointIndex = (_nextWaypointIndex + 1) % Map.MonsterWaypointCount;
                target = _map.GetMonsterWaypoint(_nextWaypointIndex);
                delta = target - _rigidbody.position;
            }

            float distance = delta.magnitude;
            float slowPercent = _monster != null ? _monster.EffectController.MoveSpeedDecreasePercent : 0f;
            float speed = Mathf.Min(_moveSpeed * (1f - slowPercent / 100f), distance / Time.fixedDeltaTime);
            _rigidbody.linearVelocity = distance > 0f ? delta / distance * speed : Vector2.zero;
        }

        public override void Stop() {
            if (_rigidbody != null) {
                _rigidbody.linearVelocity = Vector2.zero;
            }
        }
    }
}
