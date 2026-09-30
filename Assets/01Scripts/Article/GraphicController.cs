using UnityEngine;

namespace LARD.Article {
    public sealed class GraphicController : MonoBehaviour {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;

        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public Animator Animator => _animator;

        private void Awake() {
            _spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
            _animator ??= GetComponentInChildren<Animator>();
        }
    }

}
