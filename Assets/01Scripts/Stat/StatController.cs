using UnityEngine;

namespace LARD.Stat {
    public sealed class StatController : MonoBehaviour {
        [SerializeField, Min(0f)] private float _maxHealth = 100f;
        [SerializeField, Min(0f)] private float _defense;
        [SerializeField, Min(0f)] private float _moveSpeed = 3f;

        public float MaxHealth => _maxHealth;
        public float CurrentHealth { get; private set; }
        public float Defense => _defense;
        public float MoveSpeed => _moveSpeed;
        public bool IsAlive => CurrentHealth > 0f;

        private void Awake() {
            CurrentHealth = _maxHealth;
        }

        public void Initialize(float health, float defense, float moveSpeed) {
            _maxHealth = Mathf.Max(0f, health);
            _defense = Mathf.Max(0f, defense);
            _moveSpeed = Mathf.Max(0f, moveSpeed);
            CurrentHealth = _maxHealth;
        }

        public void TakeDamage(float damage) {
            CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, damage - _defense));
        }
    }
}
