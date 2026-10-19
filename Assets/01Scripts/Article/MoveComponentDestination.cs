using UnityEngine;

namespace LARD.Article {
    /// <summary>
    /// 기물을 지정한 위치로 이동시키고 이동 시작 시 진행 중인 스킬을 취소한다.
    /// </summary>
    public sealed class MoveComponentDestination : MoveComponent {
        [SerializeField, Min(0f)] private float _arrivalDistance = 0.05f;

        private Vector2 _destination;
        private bool _hasDestination;
        private SkillController _skillController;

        public bool IsMoving => _hasDestination;

        protected override void Awake() {
            base.Awake();
            _skillController = GetComponent<SkillController>();
        }

        public void SetDestination(Vector2 destination) {
            _destination = destination;
            _hasDestination = true;
            _skillController?.CancelCurrentSkill();
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
