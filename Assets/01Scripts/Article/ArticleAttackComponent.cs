using System.Collections.Generic;
using LAD.Battle;
using LAD.Skill;
using LAD.Stat;
using UnityEngine;

namespace LAD.Article {
	public sealed class ArticleAttackComponent : MonoBehaviour {
		private static readonly IReadOnlyList<string> TargetTags = new[] { "Monster" };

		private StatController statController;
		private BattleObj owner;
		private IDamageable target;
		private IDamageable committedAttackTarget;
		private float remainingCooldown;
		private SkillBase basicAttack;
		private SkillController skillController;
		private bool isPerformingAction;
		private int skillTargetIndex;
		private bool isSelectingSkillTargets;
		private readonly List<BattleObj> skillTargets = new List<BattleObj>();
		private IDamageable skillSelectionPrimaryTarget;
		private static long attackSequence;

		public IDamageable Target => target;
		internal int SkillTargetIndex => skillTargetIndex;

		public void RegisterBasicAttack(SkillBase newBasicAttack) {
			basicAttack = newBasicAttack;
		}

		public bool TryGetSkillTarget(out IDamageable skillTarget) {
			return TryGetSkillTarget(skillTargetIndex, out skillTarget);
		}

		internal void SetSkillTargetIndex(int index) {
			isSelectingSkillTargets = true;
			skillTargetIndex = Mathf.Max(0, index);
		}

		internal void SuspendSkillTargetSelection() {
			isSelectingSkillTargets = false;
			skillTargetIndex = 0;
		}

		internal void BeginSkillTargetSelection() {
			isSelectingSkillTargets = true;
			skillTargetIndex = 0;
			BuildSkillTargetList();
		}

		internal void EndSkillTargetSelection() {
			isSelectingSkillTargets = false;
			skillTargetIndex = 0;
			skillTargets.Clear();
			skillSelectionPrimaryTarget = null;
		}

		private bool TryGetSkillTarget(int index, out IDamageable skillTarget) {
			if (isSelectingSkillTargets) {
				if (index == 0 && IsValidTarget(skillSelectionPrimaryTarget)) {
					skillTarget = skillSelectionPrimaryTarget;
					return true;
				}
				int requestedIndex = Mathf.Max(0, index - 1);
				for (int candidateIndex = requestedIndex; candidateIndex < skillTargets.Count; candidateIndex++) {
					if (IsValidTarget(skillTargets[candidateIndex])) {
						skillTarget = skillTargets[candidateIndex];
						return true;
					}
				}
				if (IsValidTarget(skillSelectionPrimaryTarget)) {
					skillTarget = skillSelectionPrimaryTarget;
					return true;
				}
				skillTarget = null;
				return false;
			}
			if (IsValidTarget(committedAttackTarget)) {
				if (index == 0) {
					skillTarget = committedAttackTarget;
					return true;
				}
				return TryGetAlternativeSkillTarget(committedAttackTarget, index - 1, out skillTarget);
			}

			EnsureTargetInRange();
			if (!IsValidTarget(target)) {
				skillTarget = null;
				return false;
			}
			if (index == 0) {
				skillTarget = target;
				return true;
			}
			return TryGetAlternativeSkillTarget(target, index - 1, out skillTarget);
		}

		public void Initialize(StatController statController) {
			this.statController = statController;
			owner = GetComponentInParent<BattleObj>();
			skillController = GetComponent<SkillController>();
			ClearTarget();

			float initialAttackRange = statController != null
				? statController.GetStat(StatType.Range)
				: 0f;
			float initialSwiftness = statController != null
				? statController.GetStat(StatType.Swiftness)
				: 0f;
			float initialStrength = statController != null
				? statController.GetStat(StatType.Strength)
				: 0f;
			Debug.Log(
			          $"[ArticleAttack] 초기화 완료 - 사정거리: {initialAttackRange:0.##}, " +
			          $"신속: {initialSwiftness:0.##}, 힘: {initialStrength:0.##}",
			          this);
		}

		public void ClearTarget() {
			EndSkillTargetSelection();
			target = null;
			committedAttackTarget = null;
			remainingCooldown = 0f;
			isPerformingAction = false;
		}

		public void FinishAction() {
			committedAttackTarget = null;
			isPerformingAction = false;
		}

		/// <summary>Animation Event에서 호출되어 대기 중인 기본 공격을 실제로 발사한다.</summary>
		public void FireBasicAttack() {
			if (!isPerformingAction) {
				return;
			}

			ExecuteBasicAttack();
		}

		private void Update() {
			if (BattleManager.Current != null && !BattleManager.Current.IsRoundActive) return;
			if (statController == null) {
				return;
			}

			float attackInterval = statController.GetAttackInterval();
			if (float.IsPositiveInfinity(attackInterval)) {
				return;
			}

			remainingCooldown -= Time.deltaTime;
			if (isPerformingAction || remainingCooldown > 0f) {
				return;
			}

			EnsureTargetInRange();
			if (target == null) {
				return;
			}

			bool useSkill = skillController != null && skillController.IsSkillReady;
			FaceAttackDirection(useSkill);
			if (useSkill) {
				// 스킬은 실제 발동 직전에 대상을 다시 확인한다. 대상이 없으면 마나와
				// 쿨다운을 보존하고 다음 프레임에 타겟 탐색부터 다시 시도한다.
				if (!TryGetSkillTarget(out _)) {
					return;
				}

				isPerformingAction = skillController.TryActivateSkill();
				if (!isPerformingAction) {
					remainingCooldown = attackInterval;
				}
			}
			else {
				committedAttackTarget = target;
				isPerformingAction = owner?.GraphicController != null;
				owner?.GraphicController?.PlayAttack(statController.GetAttackRate());

				float strength = statController.GetStat(StatType.Strength);
				Debug.Log($"[ArticleAttack] 기본 공격 실행: Strength={strength:0.##}, " +
				          $"AttackInterval={attackInterval:0.###}", this);
				if (!isPerformingAction) {
					ExecuteBasicAttack();
				}

				remainingCooldown = attackInterval;
			}
		}

		private void ExecuteBasicAttack() {
			IDamageable attackTarget = committedAttackTarget;
			if (!IsValidTarget(attackTarget)) {
				committedAttackTarget = null;
				return;
			}

			long previousAttackId = owner.CurrentAttackId;
			owner.CurrentAttackId = ++attackSequence;
			try {
				float strength = statController != null
					? statController.GetStat(StatType.Strength)
					: 0f;
				if (basicAttack != null) basicAttack.Activate();
				else BattleLogic.ApplyDamage(strength, owner, attackTarget, DamageType.Physical,
					"기본 공격", AttackCategory.BasicAttack);
			}
			finally {
				owner.CurrentAttackId = previousAttackId;
				committedAttackTarget = null;
			}
		}

		private void FaceAttackDirection(bool useSkill) {
			if (owner?.GraphicController == null) {
				return;
			}

			Vector2 facingPosition;
			if (skillController == null ||
			    !skillController.TryGetFacingPosition(useSkill, out facingPosition)) {
				if (target is not Component targetComponent) {
					return;
				}

				facingPosition = targetComponent.transform.position;
			}

			float horizontalDirection = facingPosition.x - owner.transform.position.x;
			if (Mathf.Abs(horizontalDirection) <= Mathf.Epsilon) {
				return;
			}

			owner.GraphicController.SetFacingLeft(horizontalDirection < 0f);
		}

		private void EnsureTargetInRange() {
			float attackRange = statController != null
				? statController.GetStat(StatType.Range)
				: 0f;

			if (IsValidTarget(target) && IsTargetInRange(target, attackRange)) {
				return;
			}

			FindTarget(attackRange);
		}

		private void FindTarget(float attackRange) {
			target = null;
			IReadOnlyList<BattleObj> targets = TargetSearchUtil.Search(transform.position, attackRange,
			                                                           TargetTags, TargetSortMode.Nearest, owner);

			for (int i = 0; i < targets.Count; i++) {
				if (targets[i] is IDamageable damageable) {
					target = damageable;
					return;
				}
			}
		}

		private void BuildSkillTargetList() {
			skillTargets.Clear();
			EnsureTargetInRange();
			skillSelectionPrimaryTarget = IsValidTarget(committedAttackTarget) ? committedAttackTarget : target;

			float attackRange = statController != null ? statController.GetStat(StatType.Range) : 0f;
			List<BattleObj> searchedTargets = TargetSearchUtil.Search(
				transform.position, attackRange, TargetTags, TargetSortMode.Nearest, owner);
			for (int index = 0; index < searchedTargets.Count; index++) {
				BattleObj candidate = searchedTargets[index];
				if (candidate != null && IsValidTarget(candidate) && !ReferenceEquals(candidate, skillSelectionPrimaryTarget) && !skillTargets.Contains(candidate)) {
					skillTargets.Add(candidate);
				}
			}
		}

		private bool TryGetAlternativeSkillTarget(IDamageable primaryTarget, int alternativeIndex,
		                                           out IDamageable skillTarget) {
			float attackRange = statController != null ? statController.GetStat(StatType.Range) : 0f;
			BattleObj excludedTarget = primaryTarget as BattleObj;
			IReadOnlyList<BattleObj> alternatives = TargetSearchUtil.Search(
				transform.position, attackRange, TargetTags, TargetSortMode.Nearest, excludedTarget);
			if (alternativeIndex >= 0 && alternativeIndex < alternatives.Count) {
				skillTarget = alternatives[alternativeIndex];
				return true;
			}

			// 대상이 하나뿐이면 같은 대상을 다시 사용합니다.
			skillTarget = primaryTarget;
			return IsValidTarget(skillTarget);
		}

		private bool IsTargetInRange(IDamageable candidate, float attackRange) {
			if (candidate is not Component component) {
				return false;
			}

			float clampedRange = Mathf.Max(0f, attackRange);
			return ((Vector2)component.transform.position - (Vector2)transform.position).sqrMagnitude <=
			       clampedRange * clampedRange;
		}

		private static bool IsValidTarget(IDamageable candidate) {
			if (candidate == null) {
				return false;
			}

			if (candidate is Object unityObject && unityObject == null) {
				return false;
			}

			if (candidate is BattleObj battleObj && battleObj.IsDead) {
				return false;
			}

			return candidate is not Component component || component.gameObject.activeInHierarchy;
		}
	}
}
