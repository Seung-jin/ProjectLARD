using System;
using UnityEngine;

namespace LAD.Skill {
    /// <summary>투사체, 범위 장판 등 풀링되는 스킬 오브젝트의 기반 클래스입니다.</summary>
    public abstract class SkillObjBase : PoolingObject<SkillObjBase> {
        private Action<SkillObjBase, Collider2D> collisionHandler;
        private Action<SkillObjBase> effectHandler;
        private Transform poolParent;

        public SkillBase SourceSkill { get; private set; }

        internal void Initialize(
            SkillBase sourceSkill,
            Action<SkillObjBase, Collider2D> onCollision,
            Action<SkillObjBase> onEffect) {
            SourceSkill = sourceSkill;
            collisionHandler = onCollision;
            effectHandler = onEffect;
            OnInitialized();
        }

        protected virtual void OnInitialized() { }

        internal void PrepareForSpawn(Transform parent, Vector3 position, Quaternion rotation) {
            poolParent = parent;
            transform.SetParent(poolParent, true);
            transform.SetPositionAndRotation(position, rotation);
        }

        protected void ApplyEffect() {
            effectHandler?.Invoke(this);
        }

        protected virtual void OnTriggerEnter2D(Collider2D other) {
            collisionHandler?.Invoke(this, other);
        }

        public override void OnReleaseToPool() {
            SourceSkill = null;
            collisionHandler = null;
            effectHandler = null;
            transform.SetParent(poolParent, true);
            base.OnReleaseToPool();
        }
    }
}
