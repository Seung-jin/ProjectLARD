using UnityEngine;

namespace LARD.Article {
    // 기물의 방향과 그래픽 관리
    public sealed class GraphicController : MonoBehaviour {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;

        private Quaternion _initialLocalRotation;

        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public Animator Animator => _animator;

        private void Awake() {
            _initialLocalRotation = transform.localRotation;
            _spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
            _animator ??= GetComponentInChildren<Animator>();
        }

        public void FaceDirection(Vector2 direction) {
            if (direction.sqrMagnitude <= 0.0001f) {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void ResetFacing() {
            transform.localRotation = _initialLocalRotation;
        }
    }

}
