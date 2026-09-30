using UnityEngine;

namespace LARD.Article {
    public sealed class MoveComponentDestination : MoveComponent {
        [SerializeField, Min(0f)] private float _arrivalDistance = 0.05f;

        private Vector2 _destination;
        private bool _hasDestination;

        public void SetDestination(Vector2 destination) {
            _destination = destination;
            _hasDestination = true;
        }

        public override void Move() {
            if (!_hasDestination) {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 delta = _destination - _rigidbody.position;
            float distance = delta.magnitude;
            if (distance <= _arrivalDistance) {
                Stop();
                return;
            }

            float speed = Mathf.Min(_moveSpeed, distance / Time.fixedDeltaTime);
            _rigidbody.linearVelocity = delta / distance * speed;
        }

        public override void Stop() {
            _hasDestination = false;
            _rigidbody.linearVelocity = Vector2.zero;
        }
    }
}
