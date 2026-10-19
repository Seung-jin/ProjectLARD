using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Battle;
using LARD.Monster;

namespace LARD.Skill {
    // Each cast attaches its own three-tick effect to one monster.
    public sealed class AttachedDamageSkill : SkillBase {
        private CancellationTokenSource _lifetime;

        public override void Init(ArticleObj owner, SkillModel model) {
            base.Init(owner, model);
            _lifetime = new CancellationTokenSource();
        }

        public override bool TryUse() {
            if (Owner == null || Owner.Model == null || Model?.Effects == null || _lifetime == null)
                return false;

            var targets = FindTargetUtil.FindTargets(Owner.transform.position, Owner.Model.Range);
            if (targets.Count == 0 || targets[0] is not MonsterObj monster || !monster.IsAlive)
                return false;

            foreach (EffectData data in Model.Effects) {
                if (data == null || data.Effect != SkillEffectType.Damage ||
                    data.Target != EffectTargetType.NearestEnemy ||
                    data.Timing != EffectApplicationTiming.OnTick || data.HitCount <= 0 ||
                    data.DurationSeconds <= 0f)
                    continue;

                TickAsync(monster, monster.Id, Owner.Id, data.Damage, data.HitCount,
                          data.DurationSeconds / data.HitCount, _lifetime.Token).Forget();
                return true;
            }
            return false;
        }

        private async UniTaskVoid TickAsync(MonsterObj monster, int monsterId, int ownerId,
                                            float damage, int hitCount, float tickInterval,
                                            CancellationToken token) {
            try {
                for (int hit = 0; hit < hitCount; hit++) {
                    await UniTask.Delay(TimeSpan.FromSeconds(tickInterval), cancellationToken: token);
                    if (Owner == null || Owner.Id != ownerId || Owner.Model == null ||
                        monster == null || monster.Id != monsterId || !monster.IsAlive)
                        return;
                    DamageLogic.ApplyDamage(Owner, damage, monster);
                }
            } catch (OperationCanceledException) when (token.IsCancellationRequested) {
                // Despawning the owner removes its attached effects.
            }
        }

        public override void Cleanup() {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
        }

        private void OnDisable() => Cleanup();
    }
}
