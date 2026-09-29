using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LAD.Battle;
using LAD.Stat;
using LAD.Skill;
using UnityEngine;

namespace LAD.Article {
    /// <summary>
    /// \ud604\uc7ac \ub9c8\ub098\ub97c \uad00\ub9ac\ud558\uace0 \ucd5c\ub300 \ub9c8\ub098\uc5d0 \ub3c4\ub2ec\ud558\uba74 BattleObj Observer\uc5d0 \uc2a4\ud0ac \ubc1c\ub3d9\uc744 \uc54c\ub9bd\ub2c8\ub2e4.
    /// </summary>
    public sealed class SkillController : MonoBehaviour {
        private StatController statController;
        private GraphicController graphicController;
        private BattleObj owner;
        private SkillBase skill;
        private SkillBase basicAttack;
        private ArticleAttackComponent attackComponent;
        private SkillFactory ownedFactory;
        private bool isActivating;
        private bool isSkillActivationPending;
        private bool usesAnimationTrigger;
        private CancellationTokenSource repeatCancellation;
        private BattleManager observedManager;
        public float BasicAttackDamage => basicAttack != null ? basicAttack.GetBaseDamage() :
            BattleStatLogic.GetStat(owner, StatType.Strength);

        public bool IsSkillReady {
            get {
                if (isActivating || isSkillActivationPending || repeatCancellation != null || statController == null || skill == null) {
                    return false;
                }

                float maxMana = BattleStatLogic.GetStat(owner, StatType.MaxMana);
                return maxMana > 0f && statController.GetResource(ResourceType.Mana) >= maxMana;
            }
        }

        public void Initialize(
            ArticleObj article,
            ArticleAttackComponent attacks,
            StatController stats,
            GraphicController graphics,
            ArticleModel articleModel,
            int grade,
            SkillFactory factory) {
            CancelPendingSkill();
            if (observedManager != null)
                observedManager.Observer.RemoveListener(BattleManagerEventType.RoundEnded, OnRoundEnded);
            observedManager = BattleManager.Current;
            observedManager?.Observer.AddListener(BattleManagerEventType.RoundEnded, OnRoundEnded);
            if (owner != null) {
                owner.Observer.RemoveListener(BattleObjEventType.Tick, OnTick);
                owner.Observer.RemoveListener(BattleObjEventType.Releasing, OnRoundEnded);
            }

            statController = stats;
            graphicController = graphics;
            attackComponent = attacks;
            if (ownedFactory != null) {
                ownedFactory.Clear();
                ownedFactory = null;
            }
            if (factory == null) {
                ownedFactory = new SkillFactory();
                factory = ownedFactory;
            }
            owner = GetComponentInParent<BattleObj>();
            isActivating = false;
            isSkillActivationPending = false;
            usesAnimationTrigger = false;
            RegisterSkill(null);
            RegisterBasicAttack(null);

            if (statController == null || owner == null) {
                Debug.LogError($"{name}\uc758 SkillController \ucd08\uae30\ud654\uc5d0 \ud544\uc694\ud55c \uc804\ud22c \ub370\uc774\ud130\uac00 \uc5c6\uc2b5\ub2c8\ub2e4.", this);
                return;
            }

            owner.Observer.AddListener(BattleObjEventType.Tick, OnTick);
            owner.Observer.AddListener(BattleObjEventType.Releasing, OnRoundEnded);

            RegisterBasicAttack(CreateSkill(article, articleModel.BasicAttackId, grade, factory));
            RegisterSkill(CreateSkill(article, articleModel.SkillId, grade, factory));
        }

        public bool TryActivateSkill() {
            if (!IsSkillReady) {
                return false;
            }

            // 호출자가 사전에 확인했더라도 실제 발동 시점에 대상과 사거리를 다시 검증한다.
            // 실패 시 마나를 소비하지 않으며 공격 컴포넌트가 다음 프레임에 다시 판단한다.
            if (attackComponent == null || !attackComponent.TryGetSkillTarget(out _)) {
                return false;
            }

            float maxMana = BattleStatLogic.GetStat(owner, StatType.MaxMana);
            float currentMana = statController.GetResource(ResourceType.Mana);

            if (usesAnimationTrigger) {
                isSkillActivationPending = true;
                graphicController.PlaySkill();
                return true;
            }

            isActivating = true;
            try {
				if (!ActivateSkillEffects()) {
                    Debug.LogWarning(
                        $"[Skill] 효과 적용 실패: SkillId={skill.SkillId}, Grade={skill.ArticleGrade}",
                        owner);
                    return false;
                }

                statController.TryConsumeResource(ResourceType.Mana, maxMana);
                graphicController?.PlaySkill();
                owner.NotifySkillActivated();
                Debug.Log(
                    $"[Skill] 발동: SkillId={skill.SkillId}, Grade={skill.ArticleGrade}, " +
                    $"Mana={currentMana:0.##}->{statController.GetResource(ResourceType.Mana):0.##} " +
                    $"(Cost={maxMana:0.##})",
                    owner);
                return true;
            }
            finally {
                isActivating = false;
            }
        }

        public bool TryGetFacingPosition(bool useSkill, out Vector2 position) {
            SkillBase attack = useSkill ? skill : basicAttack;
            if (attack != null) {
                return attack.TryGetFacingPosition(out position);
            }

            position = default;
            return false;
        }

        public void FirePendingSkill() {
            if (!isSkillActivationPending) {
                return;
            }

            isSkillActivationPending = false;
            if (statController == null || skill == null ||
                attackComponent == null || !attackComponent.TryGetSkillTarget(out _)) {
                return;
            }

            float maxMana = BattleStatLogic.GetStat(owner, StatType.MaxMana);
            float currentMana = statController.GetResource(ResourceType.Mana);
            if (maxMana <= 0f || currentMana < maxMana) {
                return;
            }

            isActivating = true;
            try {
				if (!ActivateSkillEffects()) {
                    Debug.LogWarning(
                        $"[Skill] 지연 효과 적용 실패: SkillId={skill.SkillId}, Grade={skill.ArticleGrade}",
                        owner);
                    return;
                }

                statController.TryConsumeResource(ResourceType.Mana, maxMana);
                owner.NotifySkillActivated();
                Debug.Log(
                    $"[Skill] 지연 발동: SkillId={skill.SkillId}, Grade={skill.ArticleGrade}, " +
                    $"Mana={currentMana:0.##}->{statController.GetResource(ResourceType.Mana):0.##} " +
                    $"(Cost={maxMana:0.##})",
                    owner);
            }
            finally {
                isActivating = false;
            }
        }

        public void CancelPendingSkill() {
            isSkillActivationPending = false;
            CancellationTokenSource cancellation = repeatCancellation;
            repeatCancellation = null;
            cancellation?.Cancel();
            cancellation?.Dispose();
            attackComponent?.EndSkillTargetSelection();
        }

        private void OnRoundEnded(params object[] args) => CancelPendingSkill();

        public void RegisterSkill(SkillBase newSkill) {
            graphicController?.ClearAnimationTriggers();
            if (skill != null) {
                skill.Dispose();
            }
            skill = newSkill;
            usesAnimationTrigger = skill != null &&
                                   skill.BindAnimationTriggers(graphicController, FirePendingSkill);
        }

		private void RegisterBasicAttack(SkillBase newBasicAttack) {
			if (basicAttack != null) {
				basicAttack.Dispose();
			}
			basicAttack = newBasicAttack;
			attackComponent?.RegisterBasicAttack(basicAttack);
		}

		private bool ActivateSkillEffects() {
			if (skill == null || attackComponent == null) {
				return false;
			}

			bool activated = false;
			float additionalActivations = BattleStatLogic.GetStat(owner, StatType.SkillActivationCount);
			int activationCount = Mathf.Max(1, 1 + Mathf.FloorToInt(additionalActivations));
			attackComponent.BeginSkillTargetSelection();
			try {
				attackComponent.SetSkillTargetIndex(0);
				BattleCombatLog.RecordSkill(owner, $"{skill.GetType().Name} (ID {skill.SkillId})", 0);
				activated = skill.Activate();
				attackComponent.SuspendSkillTargetSelection();
				if (activated && activationCount > 1 &&
                    (BattleManager.Current == null || BattleManager.Current.IsRoundActive)) {
					repeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
					ActivateSkillWithDelay(activationCount, repeatCancellation).Forget();
				}
				else attackComponent.EndSkillTargetSelection();
			}
			catch {
				attackComponent.EndSkillTargetSelection();
				throw;
			}

			return activated;
		}

		private async UniTask ActivateSkillWithDelay(int activationCount, CancellationTokenSource cancellation) {
			CancellationToken token = cancellation.Token;
			try {
				for (int index = 1; index < activationCount; index++) {
					await UniTask.Delay(TimeSpan.FromSeconds(0.5f),
						DelayType.DeltaTime, PlayerLoopTiming.Update, token);
					if (attackComponent == null || skill == null || owner == null || owner.IsDead) return;
					attackComponent.SetSkillTargetIndex(index);
					BattleCombatLog.RecordSkill(owner, $"{skill.GetType().Name} (ID {skill.SkillId})", index);
					skill.Activate();
					attackComponent.SuspendSkillTargetSelection();
				}
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested) { }
			finally {
				if (ReferenceEquals(repeatCancellation, cancellation)) {
					repeatCancellation = null;
					attackComponent?.EndSkillTargetSelection();
					cancellation.Dispose();
				}
			}
		}

        private SkillBase CreateSkill(ArticleObj article, int skillId, int grade, SkillFactory factory) {
            if (skillId <= 0) {
                return null;
            }
            if (!SkillDataSet.TryGet(skillId, out SkillModel model) || !model.TryGetGrade(grade, out _)) {
                Debug.LogError($"Skill {skillId} 또는 {grade}성 데이터를 찾을 수 없습니다.", this);
                return null;
            }
            return factory?.Create(article, attackComponent, model, grade);
        }

        private void OnTick(params object[] args) {
            if (statController == null || args.Length < 2 || args[1] is not float deltaTime ||
                owner is not ArticleObj article || !article.IsOnBattlefield ||
                (BattleManager.Current != null && !BattleManager.Current.IsRoundActive)) return;
            float mana = statController.GetResource(ResourceType.Mana) +
                         BattleStatLogic.GetStat(owner, StatType.ManaRegen) * Mathf.Max(0f, deltaTime);
            statController.SetResource(ResourceType.Mana, Mathf.Min(mana, BattleStatLogic.GetStat(owner, StatType.MaxMana)));
        }

        private void OnDestroy() {
            CancelPendingSkill();
            if (observedManager != null)
                observedManager.Observer.RemoveListener(BattleManagerEventType.RoundEnded, OnRoundEnded);
            if (skill != null) {
                skill.Dispose();
            }
            if (basicAttack != null) {
                basicAttack.Dispose();
            }
            ownedFactory?.Clear();
            ownedFactory = null;
            isSkillActivationPending = false;
            usesAnimationTrigger = false;
            graphicController?.ClearAnimationTriggers();

            if (owner != null) {
                owner.Observer.RemoveListener(BattleObjEventType.Tick, OnTick);
                owner.Observer.RemoveListener(BattleObjEventType.Releasing, OnRoundEnded);
            }
        }
    }
}
