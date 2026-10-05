using LARD.Battle;
using UnityEngine;

namespace LARD.Monster {
    public sealed class MoveComponentMonsterPath : MoveComponent {
        [SerializeField, Min(0f)] private float _arrivalDistance = 0.05f;

        private Map _map;
        private int _nextWaypointIndex;

        public void SetPath(Map map) {
            _map = map;
            _nextWaypointIndex = 1;
            Stop();
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
            float speed = Mathf.Min(_moveSpeed, distance / Time.fixedDeltaTime);
            _rigidbody.linearVelocity = distance > 0f ? delta / distance * speed : Vector2.zero;
        }

        public override void Stop() {
            if (_rigidbody != null) {
                _rigidbody.linearVelocity = Vector2.zero;
            }
        }
    }
}
