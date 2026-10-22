using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LARD.Buff;
using LARD.Skill;
using UnityEngine;

namespace LARD.Article {
    /// <summary>
    /// 기물의 스킬 인스턴스, 공격 주기와 사용 중 취소를 관리한다.
    /// </summary>
    public sealed class SkillController : MonoBehaviour {
        private CancellationTokenSource _attackLoopCancellation;
        private CancellationTokenSource _skillUseCancellation;
        private MoveComponentDestination _movement;

        public SkillModel Model { get; private set; }
        public SkillBase Skill { get; private set; }

        private void Awake() {
            _movement = GetComponent<MoveComponentDestination>();
        }

        /// <summary>이동 중이 아닐 때 현재 스킬의 사용을 시도한다.</summary>
        public bool TryUse() {
            if (Skill == null || IsMoving()) return false;
            bool used = Skill.TryUse();
            if (used) Skill.Owner?.GraphicController?.PlayAttack();
            return used;
        }

        /// <summary>기존 스킬을 정리하고 새 스킬을 연결하며 활성 상태면 공격 루프를 시작한다.</summary>
        public void SetSkill(SkillModel model, SkillBase skill) {
            ClearSkill();
            Model = model;
            Skill = skill;
            if (isActiveAndEnabled) {
                StartAttackLoop();
            }
        }

        /// <summary>현재 진행 중인 스킬 사용을 취소한다.</summary>
        public void CancelCurrentSkill() {
            _skillUseCancellation?.Cancel();
        }

        /// <summary>공격 루프를 중단하고 스킬 인스턴스를 정리한다.</summary>
        public void ClearSkill() {
            StopAttackLoop();
            if (Skill != null) {
                Skill.Cleanup();
                Destroy(Skill);
            }

            Skill = null;
            Model = null;
        }

        private void OnEnable() {
            if (Skill != null) {
                StartAttackLoop();
            }
        }

        private void OnDisable() {
            StopAttackLoop();
        }

        private void StartAttackLoop() {
            if (Skill == null || Skill.Owner == null || Skill.Owner.Model == null ||
                Skill.Owner.Model.AttackSpeed <= 0f) {
                return;
            }

            StopAttackLoop();
            var cancellation = new CancellationTokenSource();
            _attackLoopCancellation = cancellation;
            AttackLoopAsync(Skill, cancellation).Forget();
        }

        private void StopAttackLoop() {
            _attackLoopCancellation?.Cancel();
            CancelCurrentSkill();
        }

        private bool IsMoving() {
            return _movement != null && _movement.IsMoving;
        }

        /// <summary>공격 속도와 강화 효과를 반영해 스킬 사용 주기를 진행한다.</summary>
        private async UniTaskVoid AttackLoopAsync(SkillBase skill, CancellationTokenSource cancellation) {
            CancellationToken token = cancellation.Token;
            float attackProgress = 1f;
            try {
                while (!token.IsCancellationRequested) {
                    if (attackProgress < 1f || IsMoving()) {
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                        float speed = skill.Owner.Model.AttackSpeed *
                                      (1f + skill.Owner.BuffController.GetTotalValue(BuffType.AttackSpeedIncrease) / 100f);
                        attackProgress = Mathf.Min(1f, attackProgress + Time.deltaTime * Mathf.Max(0f, speed));
                        continue;
                    }

                    if (skill.Owner.Model.AttackSpeed <= 0f) {
                        return;
                    }

                    attackProgress = 0f;
                    using var skillCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                    _skillUseCancellation = skillCancellation;
                    try {
                        bool used = await skill.TryUseAsync(skillCancellation.Token);
                        if (used && !token.IsCancellationRequested) {
                            skill.Owner?.GraphicController?.PlayAttack();
                        }
                    } catch (OperationCanceledException) when (skillCancellation.IsCancellationRequested) {
                        // Movement or despawning interrupted the current skill.
                    } finally {
                        if (_skillUseCancellation == skillCancellation) {
                            _skillUseCancellation = null;
                        }
                    }
                }
            } catch (OperationCanceledException) when (token.IsCancellationRequested) {
                // The article was disabled, returned to the pool, or given another skill.
            } finally {
                if (_attackLoopCancellation == cancellation) {
                    _attackLoopCancellation = null;
                }
                cancellation.Dispose();
            }
        }

        private void OnDestroy() {
            ClearSkill();
        }
    }

}
