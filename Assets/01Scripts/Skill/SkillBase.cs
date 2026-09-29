using System;
using LAD.Article;
using LAD.Battle;
using LAD.Stat;
using UnityEngine;

namespace LAD.Skill {
    public static class SkillAnimationTriggerKeys {
        public const string ShotObj = "ShotObj";
    }

    /// <summary>기물 스킬 실행 로직의 기반 클래스입니다.</summary>
    public abstract class SkillBase {
        protected ArticleObj Owner { get; private set; }
        protected ArticleAttackComponent AttackComponent { get; private set; }
        protected StatController StatController => Owner?.StatController;
        protected SkillModel Model { get; private set; }
		protected SkillGradeData GradeData { get; private set; }
        private SkillFactory factory;
		public int SkillId => Model != null ? Model.Id : 0;
		public SkillTargetType TargetType => Model != null ? Model.TargetType : SkillTargetType.Monster;
		public int ArticleGrade => GradeData != null ? GradeData.ArticleGrade : 1;
		protected int TargetSelectionIndex => AttackComponent != null ? AttackComponent.SkillTargetIndex : 0;

        public void Initialize(ArticleObj owner, ArticleAttackComponent attackComponent,
                               SkillModel model, SkillGradeData gradeData, SkillFactory skillFactory) {
            Owner = owner;
            AttackComponent = attackComponent;
            Model = model;
            GradeData = gradeData;
            factory = skillFactory;
        }

        public abstract bool Activate();

        /// <summary>치명타, 대상 방어, 추가 발동 효과를 적용하기 전 기본 공격 원본 피해입니다.</summary>
        public virtual float GetBaseDamage() => CalculateScaledDamage(StatType.Strength);

        /// <summary>공격 애니메이션이 바라볼 월드 좌표를 반환합니다.</summary>
        public virtual bool TryGetFacingPosition(out Vector2 position) {
            if (TryGetCurrentTarget(out IDamageable target) && target is Component component) {
                position = component.transform.position;
                return true;
            }

            position = default;
            return false;
        }

        /// <summary>스킬별 애니메이션 트리거 액션을 그래픽 컨트롤러에 등록합니다.</summary>
        public virtual bool BindAnimationTriggers(GraphicController graphicController, Action shotObjAction) {
            return false;
        }

        public virtual void Dispose() { }

        /// <summary>투사체/스킬 오브젝트 생성 타이밍에 사용할 표준 ShotObj 트리거를 등록합니다.</summary>
        protected static bool ShotObj(GraphicController graphicController, Action action) {
            if (graphicController == null || action == null) {
                return false;
            }

            graphicController.RegisterAnimationTrigger(SkillAnimationTriggerKeys.ShotObj, action);
            return true;
        }

        protected bool TryGetCurrentTarget(out IDamageable target) {
            if (AttackComponent == null) {
                target = null;
                return false;
            }

            return AttackComponent.TryGetSkillTarget(out target);
        }

        protected bool ActivateEffect(EffectData effectData, BattleObj target) {
            if (effectData == null) {
                Debug.LogError($"Skill {SkillId}에서 발동할 EffectData가 없습니다.", Owner);
                return false;
            }
            return effectData.Activate(Owner, target);
        }

        /// <summary>주 스탯 계수와 선택적인 특화/신속 계수를 합산해 원본 피해를 계산합니다.</summary>
        protected float CalculateScaledDamage(StatType scalingStat, string ratioKey = SkillValueKeys.DamageRatio) {
            if (StatController == null || GradeData == null ||
                !GradeData.TryGetValue(ratioKey, out float ratio)) {
                Debug.LogError($"Skill {SkillId}의 대미지 계수 '{ratioKey}'를 찾을 수 없습니다.", Owner);
                return 0f;
            }

            float damage = BattleStatLogic.GetStat(Owner, scalingStat) * Mathf.Max(0f, ratio);
            if (GradeData.TryGetValue(SkillValueKeys.SpecializationDamageRatio, out float specializationRatio))
                damage += BattleStatLogic.GetStat(Owner, StatType.Specialization) * Mathf.Max(0f, specializationRatio);
            if (GradeData.TryGetValue(SkillValueKeys.SwiftnessDamageRatio, out float swiftnessRatio))
                damage += BattleStatLogic.GetStat(Owner, StatType.Swiftness) * Mathf.Max(0f, swiftnessRatio);
            return damage;
        }

        protected SkillObjBase SpawnSkillObject(string prefabKey, Vector3 position, Quaternion rotation,
                                                Action<SkillObjBase, Collider2D> onCollision = null, Action<SkillObjBase> onEffect = null) {
            if (factory == null || GradeData == null ||
                !GradeData.TryGetPrefab(prefabKey, out SkillObjBase prefab)) {
                return null;
            }

            SkillObjBase skillObject = factory.Spawn(prefab, position, rotation);
            int lifeVersion = Owner.LifeVersion;
            long attackId = Owner.CurrentAttackId;
            BattleManager manager = BattleManager.Current;
            int round = manager != null ? manager.CurrentRoundId : 0;
            bool IsValidSource() => Owner != null && !Owner.IsDead && Owner.LifeVersion == lifeVersion &&
                Owner.IsOnBattlefield && (manager == null || (manager.IsRoundActive && manager.CurrentRoundId == round));
            void InvokeWithContext(SkillObjBase obj, Action action) {
                if (!IsValidSource()) { obj.Release(); return; }
                long previous = Owner.CurrentAttackId;
                Owner.CurrentAttackId = attackId;
                try { action(); }
                finally { Owner.CurrentAttackId = previous; }
            }
            skillObject?.Initialize(this,
                onCollision != null ? (obj, collider) => InvokeWithContext(obj, () => onCollision(obj, collider)) : null,
                onEffect != null ? obj => InvokeWithContext(obj, () => onEffect(obj)) : null);
            return skillObject;
        }

        /// <summary>직접 피격 이펙트를 대상의 공통 피격 기준점에 붙여 생성합니다.</summary>
        protected SkillObjBase SpawnAttachedHitEffect(
            string prefabKey,
            BattleObj target,
            Quaternion rotation,
            Action<SkillObjBase, Collider2D> onCollision = null,
            Action<SkillObjBase> onEffect = null) {
            if (target == null) {
                return null;
            }

            Transform anchor = target.HitEffectPosition;
            SkillObjBase skillObject = SpawnSkillObject(
                prefabKey, anchor.position, rotation, onCollision, onEffect);
            if (skillObject != null) {
                skillObject.transform.SetParent(anchor, true);
            }

            return skillObject;
        }
    }
}
