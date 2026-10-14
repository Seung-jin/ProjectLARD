using UnityEngine;

namespace LARD.Article {
    // 기물의 방향과 그래픽 관리
    public sealed class GraphicController : MonoBehaviour {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;

        private Quaternion _initialLocalRotation;
        private bool _initialFlipX;

        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public Animator Animator => _animator;

        private void Awake() {
            _initialLocalRotation = transform.localRotation;
            _spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
            _animator ??= GetComponentInChildren<Animator>();
            if (_spriteRenderer != null) {
                _initialFlipX = _spriteRenderer.flipX;
            }
        }

        public void FaceDirection(Vector2 direction) {
            if (_spriteRenderer == null || Mathf.Abs(direction.x) <= 0.0001f) {
                return;
            }

            _spriteRenderer.flipX = direction.x < 0f;
        }

        public void ResetFacing() {
            transform.localRotation = _initialLocalRotation;
            if (_spriteRenderer != null) {
                _spriteRenderer.flipX = _initialFlipX;
            }
        }
    }

}
