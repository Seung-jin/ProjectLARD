using System;
using System.Collections.Generic;
using UnityEngine;

namespace LAD.Article {
    /// <summary>
    /// 기물의 Animator 파라미터를 제어하는 그래픽 전용 컴포넌트다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class GraphicController : MonoBehaviour {
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int SkillHash = Animator.StringToHash("Skill");
        private static readonly int DieHash = Animator.StringToHash("Die");

        [SerializeField] private Animator animator;
		private ArticleAttackComponent attackComponent;
		private SkillController skillController;
		private readonly Dictionary<string, Action> animationTriggerActions =
			new Dictionary<string, Action>(StringComparer.Ordinal);

        public Animator Animator => animator;

        private void Awake() {
            animator ??= GetComponent<Animator>();
			attackComponent = GetComponentInParent<ArticleAttackComponent>();
			skillController = GetComponentInParent<SkillController>();
        }

        /// <summary>
        /// 오른쪽을 바라보는 원본 리소스를 기준으로 Y축 회전을 사용해 방향을 전환한다.
        /// </summary>
        public void SetFacingLeft(bool isFacingLeft) {
            transform.localRotation = Quaternion.Euler(0f, isFacingLeft ? 180f : 0f, 0f);
        }

        public void PlayIdle() {
            if (!TryGetAnimator()) return;
            animator.SetFloat(MoveSpeedHash, 0f);
        }

        public void PlayMove(float normalizedSpeed = 1f) {
            if (!TryGetAnimator()) return;
            animator.SetFloat(MoveSpeedHash, Mathf.Max(0.01f, normalizedSpeed));
        }

		public void PlayAttack(float attackRate = 1f) {
			if (!TryGetAnimator()) return;
			animator.speed = Mathf.Max(0.01f, attackRate);
            SetTrigger(AttackHash);
        }

        public void PlaySkill() {
			if (!TryGetAnimator()) return;
			animator.speed = 1f;
            SetTrigger(SkillHash);
        }

		/// <summary>Animation Event called at the end of attack and skill clips.</summary>
		public void Finish() {
			if (TryGetAnimator()) {
				animator.speed = 1f;
			}
			skillController ??= GetComponentInParent<SkillController>();
			skillController?.CancelPendingSkill();
			attackComponent ??= GetComponentInParent<ArticleAttackComponent>();
			attackComponent?.FinishAction();
		}

		/// <summary>Animation Event called at the attack impact frame.</summary>
		public void FireBasicAttack() {
			attackComponent ??= GetComponentInParent<ArticleAttackComponent>();
			attackComponent?.FireBasicAttack();
		}

		/// <summary>애니메이션 이벤트에서 호출할 액션을 문자열 키로 등록하거나 교체합니다.</summary>
		public void RegisterAnimationTrigger(string triggerKey, Action action) {
			if (string.IsNullOrWhiteSpace(triggerKey) || action == null) return;
			animationTriggerActions[triggerKey] = action;
		}

		public bool UnregisterAnimationTrigger(string triggerKey) {
			return !string.IsNullOrWhiteSpace(triggerKey) && animationTriggerActions.Remove(triggerKey);
		}

		public void ClearAnimationTriggers() {
			animationTriggerActions.Clear();
		}

		/// <summary>Animation Event가 전달한 문자열 키에 등록된 액션을 실행합니다.</summary>
		public void TriggerAnimationAction(string triggerKey) {
			if (string.IsNullOrWhiteSpace(triggerKey)) return;
			if (animationTriggerActions.TryGetValue(triggerKey, out Action action)) {
				action.Invoke();
			}
		}

        public void PlayDie() {
            SetTrigger(DieHash);
        }

        public void ResetToIdle() {
            if (!TryGetAnimator()) return;
            SetFacingLeft(false);
            animator.ResetTrigger(AttackHash);
            animator.ResetTrigger(SkillHash);
            animator.ResetTrigger(DieHash);
            animator.SetFloat(MoveSpeedHash, 0f);
			animator.speed = 1f;
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
        }

        private void SetTrigger(int triggerHash) {
            if (!TryGetAnimator()) return;
            animator.SetTrigger(triggerHash);
        }

        private bool TryGetAnimator() {
            animator ??= GetComponent<Animator>();
            if (animator != null) return true;

            Debug.LogError($"{name}에 Animator가 지정되지 않았습니다.", this);
            return false;
        }
    }
}
