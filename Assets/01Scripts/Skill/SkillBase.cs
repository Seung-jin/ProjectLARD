using System.Threading;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Skill.Effect;
using UnityEngine;

namespace LARD.Skill {
    public static class SkillAnimationTriggerKeys {
        public const string Attack = "Attack";
    }

    public class SkillBase : ScriptableObject {
        public ArticleObj Owner { get; private set; }
        public SkillModel Model { get; private set; }
        protected SkillFactory Factory { get; private set; }

        internal void SetFactory(SkillFactory factory) {
            Factory = factory;
        }

        public virtual void Initialize(ArticleObj owner, SkillModel model) {
            Owner = owner;
            Model = model;
        }

        public virtual void Cleanup() { }

        public virtual UniTask<bool> TryUseAsync(CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(TryUse());
        }

        public virtual bool TryUse() {
            if (Owner == null || Owner.Model == null || Model == null || Model.Effects == null) {
                return false;
            }

            bool applied = false;
            foreach (EffectData data in Model.Effects) {
                if (data == null || data.Timing != EffectApplicationTiming.Immediate || data.HitCount < 1) {
                    continue;
                }

                SkillEffectBase effect = SkillEffectFactory.Create(data.Effect);
                if (effect == null || data.Target != EffectTargetType.NearestEnemy) {
                    continue;
                }

                var targets = FindTargetUtil.FindTargets(Owner.transform.position, Owner.Model.Range);
                if (targets.Count == 0) {
                    continue;
                }

                IAttackTarget target = targets[0];
                for (int hit = 0; hit < data.HitCount && target.IsAlive; hit++) {
                    effect.Apply(Owner, data, target);
                    applied = true;
                }
            }

            return applied;
        }
    }

}
