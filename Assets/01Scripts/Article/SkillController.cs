using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LARD.Skill;
using UnityEngine;

namespace LARD.Article {
    public sealed class SkillController : MonoBehaviour {
        private CancellationTokenSource _attackLoopCancellation;
        private CancellationTokenSource _skillUseCancellation;
        private MoveComponentDestination _movement;

        public SkillModel Model { get; private set; }
        public SkillBase Skill { get; private set; }

        private void Awake() {
            _movement = GetComponent<MoveComponentDestination>();
        }

        public bool TryUse() {
            return Skill != null && !IsMoving() && Skill.TryUse();
        }

        public void SetSkill(SkillModel model, SkillBase skill) {
            ClearSkill();
            Model = model;
            Skill = skill;
            if (isActiveAndEnabled) {
                StartAttackLoop();
            }
        }

        public void CancelCurrentSkill() {
            _skillUseCancellation?.Cancel();
        }

        public void ClearSkill() {
            StopAttackLoop();
            if (Skill != null) {
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

        private async UniTaskVoid AttackLoopAsync(SkillBase skill, CancellationTokenSource cancellation) {
            CancellationToken token = cancellation.Token;
            float nextAttackTime = Time.time;
            try {
                while (!token.IsCancellationRequested) {
                    if (IsMoving()) {
                        await UniTask.WaitUntil(() => !IsMoving(), cancellationToken: token);
                    }

                    float waitSeconds = nextAttackTime - Time.time;
                    if (waitSeconds > 0f) {
                        await UniTask.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken: token);
                        continue;
                    }

                    float attackSpeed = skill.Owner.Model.AttackSpeed;
                    if (attackSpeed <= 0f) {
                        return;
                    }

                    nextAttackTime = Time.time + 1f / attackSpeed;
                    using var skillCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                    _skillUseCancellation = skillCancellation;
                    try {
                        await skill.TryUseAsync(skillCancellation.Token);
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
