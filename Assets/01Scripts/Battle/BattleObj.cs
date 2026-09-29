using System.Collections.Generic;
using LAD.Buff;
using LAD.Article;
using LAD.Stat;
using LSJ.Util;
using UnityEngine;

namespace LAD.Battle {
    /// <summary>
    /// <see cref="BattleObj"/>에서 발생하는 공통 전투 이벤트입니다.
    /// </summary>
    public enum BattleObjEventType {
        Initialized,
        ResourceChanged,
        Attack,
        DamageDealt,
        Damaged,
        Died,
        SkillActivated,
        SkillHit,
        Tick,
        BasicAttackHit,
        BeforeDamage,
        Releasing,
    }

    /// <summary>
    /// 전투에 참여하는 모든 오브젝트의 기반 클래스다.
    /// 이동과 스탯에 대한 공통 기능은 추후 이 클래스에 추가한다.
    /// </summary>
    public abstract class BattleObj : PoolingObject<BattleObj>, IDamageable {
        [Header("Movement")]
        [Tooltip("이 전투 오브젝트가 사용할 이동 컴포넌트입니다.")]
        [SerializeField] protected MoveComponent moveComponent;

        [Header("Graphic")]
        [Tooltip("이 전투 오브젝트의 애니메이션을 제어하는 그래픽 컴포넌트입니다.")]
        [SerializeField] protected GraphicController graphicController;

        [Header("Hit Effect")]
        [Tooltip("이 전투 오브젝트에 직접 적중한 이펙트가 붙을 기준점입니다.")]
        [SerializeField] private Transform hitEffectPosition;

        [Header("Damage Text")]
        [Tooltip("피해량을 표시할 DamageText 프리팹입니다.")]
        [SerializeField] private LSJ.Battle.DamageText damageTextPrefab;
        [Tooltip("DamageText가 나타날 월드 위치의 기준점입니다.")]
        [SerializeField] private Transform damageTextPosition;

        private ObjectPooler<LSJ.Battle.DamageText> damageTextPool;

        public MoveComponent MoveComponent => moveComponent;
        public GraphicController GraphicController => graphicController;
        public Transform HitEffectPosition => hitEffectPosition != null ? hitEffectPosition : transform;
        public StatController StatController { get; private set; }
        public BuffController BuffController { get; private set; }
        public LAD.Effects.EffectController Effects { get; private set; }
        public int LifeVersion { get; private set; }
        internal long CurrentAttackId { get; set; }
        public Observer<BattleObjEventType> Observer { get; } = new Observer<BattleObjEventType>();

        /// <summary>0이면 성급 UI를 표시하지 않는다.</summary>
        public virtual int Grade => 0;

        /// <summary>체력 UI 사용 여부입니다.</summary>
        public virtual bool ShowHealthUI => true;

        public bool IsDead { get; private set; }
        public LAD.Skill.BasicAttackEnhancementController BasicAttackEnhancements { get; } = new();

        private void OnEnable() {
            Observer.RemoveListener(BattleObjEventType.Damaged, OnDamaged);
            Observer.AddListener(BattleObjEventType.Damaged, OnDamaged);
        }

        private void OnDisable() {
            Observer.RemoveListener(BattleObjEventType.Damaged, OnDamaged);
        }

        protected virtual void OnDestroy() {
            Effects?.Dispose();
            Observer.ClearAll();
        }

        private void OnDamaged(params object[] args) {
            if (args.Length < 2 || args[1] is not float damage || damage <= 0f) {
                return;
            }

            if (damageTextPrefab == null || damageTextPosition == null) {
                return;
            }

            damageTextPool ??= new ObjectPooler<LSJ.Battle.DamageText>(damageTextPrefab);
            LSJ.Battle.DamageText damageText = damageTextPool.Get();
            damageText.Build(Mathf.RoundToInt(damage), damageTextPosition.position);
        }

        protected void InitializeStats(IReadOnlyDictionary<StatType, float> stats) {
            Effects?.Dispose();
            LifeVersion++;
            BasicAttackEnhancements.Clear();
            if (StatController != null) {
                StatController.OnResourceChanged -= OnResourceChanged;
            }

            StatController = new StatController(stats);
            StatController.OnResourceChanged += OnResourceChanged;
            BuffController = new BuffController(StatController);
            Effects = new LAD.Effects.EffectController(this);
            IsDead = false;
            graphicController?.ResetToIdle();
            Observer.TriggerEvent(BattleObjEventType.Initialized, this);
        }

        private void OnResourceChanged(ResourceType type, float value) {
            Observer.TriggerEvent(BattleObjEventType.ResourceChanged, this, type, value);
        }

        public void SetMoveSpeed(float moveSpeed) {
            if (moveComponent == null) {
                Debug.LogError($"{name}에 MoveComponent가 지정되지 않았습니다.", this);
                return;
            }

            moveComponent.SetMoveSpeed(moveSpeed);
        }

        public virtual void TakeDamage(float damage) {
            if (IsDead) {
                return;
            }

            if (StatController == null) {
                Debug.LogError($"{name}의 StatController가 초기화되지 않았습니다.", this);
                return;
            }

            damage = Mathf.Max(0f, damage);
            float shield = StatController.GetResource(ResourceType.Shield);
            float absorbedDamage = Mathf.Min(shield, damage);

            if (absorbedDamage > 0f) {
                StatController.AddResource(ResourceType.Shield, -absorbedDamage);
            }

            float healthDamage = damage - absorbedDamage;
            if (healthDamage > 0f) {
                StatController.AddResource(ResourceType.Health, -healthDamage);
            }

            float appliedDamage = absorbedDamage + healthDamage;
            Observer.TriggerEvent(BattleObjEventType.Damaged, this, appliedDamage);

            if (StatController.GetResource(ResourceType.Health) <= 0f) {
                Die();
            }
        }

        public void NotifyAttack(IDamageable target) {
            Observer.TriggerEvent(BattleObjEventType.Attack, this, target);
        }

        public void NotifyDamageDealt(AttackParameters parameters) {
            Observer.TriggerEvent(BattleObjEventType.DamageDealt, parameters);
            BattleManager.Current?.Observer.TriggerEvent(BattleManagerEventType.DamageDealt, parameters);
            if (parameters.Damage <= 0f) return;
            if (parameters.Category == AttackCategory.BasicAttack) {
                Observer.TriggerEvent(BattleObjEventType.BasicAttackHit, parameters);
                BattleManager.Current?.Observer.TriggerEvent(BattleManagerEventType.BasicAttackHit, parameters);
            }
            else if (parameters.Category == AttackCategory.Skill) {
                Observer.TriggerEvent(BattleObjEventType.SkillHit, parameters);
                BattleManager.Current?.Observer.TriggerEvent(BattleManagerEventType.SkillHit, parameters);
            }
        }

        public void NotifySkillActivated() {
            Observer.TriggerEvent(BattleObjEventType.SkillActivated, this);
			BattleManager.Current?.Observer.TriggerEvent(BattleManagerEventType.SkillActivated, this);
        }

        protected virtual void Die() {
            if (IsDead) {
                return;
            }

            IsDead = true;
            Observer.TriggerEvent(BattleObjEventType.Died, this);
            Release();
        }

        protected virtual void Update() {
            float deltaTime = Time.deltaTime;
            BasicAttackEnhancements.Update(deltaTime);
            BuffController?.Update(deltaTime);

            if (!IsDead && StatController != null) {
                Observer.TriggerEvent(BattleObjEventType.Tick, this, deltaTime);
				BattleManager.Current?.Observer.TriggerEvent(BattleManagerEventType.Tick, this, deltaTime);
            }
        }

        public override void OnReleaseToPool() {
            Observer.TriggerEvent(BattleObjEventType.Releasing, this);
            LifeVersion++;
            Effects?.Dispose();
            ReleaseAttachedHitEffects();
            BasicAttackEnhancements.Clear();
            if (moveComponent != null) {
                moveComponent.Stop();
            }

            BuffController?.Clear();
            Observer.ClearAll();
            graphicController?.ResetToIdle();

            base.OnReleaseToPool();
        }

        private void ReleaseAttachedHitEffects() {
            LAD.Skill.SkillObjBase[] effects =
                transform.GetComponentsInChildren<LAD.Skill.SkillObjBase>(true);
            for (int index = 0; index < effects.Length; index++) {
                effects[index]?.Release();
            }
        }
    }
}
