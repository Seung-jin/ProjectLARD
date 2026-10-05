using System;
using LARD.Battle;
using LARD.Stat;
using LARD.Skill;
using LSJ.Util;
using UnityEngine;

namespace LARD.Monster {
    public enum MonsterEventType {
        HealthChanged,
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(StatController), typeof(MoveComponentMonsterPath))]
    public sealed class MonsterObj : PoolingObject<MonsterObj>, IAttackTarget {
        [SerializeField] private MonsterModel _model;
        [SerializeField] private StatController _statController;
        [SerializeField] private MoveComponentMonsterPath _moveComponent;

        private Map _map;

        public event Action<MonsterObj> Released;
        public Observer<MonsterEventType> MonsterObserver { get; private set; } = new();

        public MonsterModel Model => _model;
        public StatController StatController => _statController;
        public MoveComponentMonsterPath MoveComponent => _moveComponent;
        public Transform TargetTransform => transform;
        public bool IsAlive => _statController != null && _statController.IsAlive;

        public void TakeDamage(float damage) {
            if (!IsAlive) {
                return;
            }

            _statController.TakeDamage(damage);
            MonsterObserver.TriggerEvent(MonsterEventType.HealthChanged);
            if (!IsAlive) {
                Release();
            }
        }

        private void Awake() {
            _statController ??= GetComponent<StatController>();
            _moveComponent ??= GetComponent<MoveComponentMonsterPath>();
        }

        private void Start() {
            if (_map == null) {
                Initialize(_model, FindFirstObjectByType<Map>());
            }
        }

        public void Initialize(MonsterModel model, Map map) {
            _model = model;
            _map = map;

            if (model != null) {
                _statController.Initialize(model.Health, model.Defense, model.MoveSpeed);
                _moveComponent.SetMoveSpeed(_statController.MoveSpeed);
                MonsterObserver.TriggerEvent(MonsterEventType.HealthChanged);
            }

            if (map == null) {
                Debug.LogWarning("[Monster] Map is missing; monster cannot follow its path.", this);
                _moveComponent.SetPath(null);
                return;
            }

            transform.position = map.MonsterSpawnPosition;
            _moveComponent.SetPath(map);
        }

        public override void OnGetFromPool() {
            base.OnGetFromPool();
            _moveComponent.enabled = true;
        }

        public override void OnReleaseToPool() {
            _moveComponent.SetPath(null);
            _moveComponent.enabled = false;
            _map = null;
            _model = null;
            Released?.Invoke(this);
            base.OnReleaseToPool();
        }

        private void OnDisable() {
            _moveComponent?.Stop();
        }
    }
}
