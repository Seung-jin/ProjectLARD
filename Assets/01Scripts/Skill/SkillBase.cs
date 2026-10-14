using System.Threading;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Skill.Effect;
using LARD.Monster;
using System.Collections.Generic;
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

        public virtual void Init(ArticleObj owner, SkillModel model) {
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

            var targets = FindTargetUtil.FindTargets(Owner.transform.position, Owner.Model.Range);
            return targets.Count > 0 && ApplyToTarget(targets[0], EffectTargetType.NearestEnemy);
        }

        protected bool ApplyToTarget(IAttackTarget target, EffectTargetType damageTarget) {
            if (target == null || !target.IsAlive || Model?.Effects == null) return false;

            float healthBefore = target is MonsterObj monster ? monster.StatController.CurrentHealth : 0f;
            bool applied = false;
            foreach (EffectData data in Model.Effects) {
                if (data == null || data.Timing != EffectApplicationTiming.Immediate ||
                    data.Target != damageTarget || data.HitCount < 1) {
                    continue;
                }

                SkillEffectBase effect = SkillEffectFactory.Create(data.Effect);
                if (effect == null) continue;
                for (int hit = 0; hit < data.HitCount && target.IsAlive; hit++) {
                    effect.Apply(Owner, data, target);
                    applied = true;
                }
            }

            if (!applied || target is not MonsterObj damaged ||
                damaged.StatController.CurrentHealth >= healthBefore) return applied;

            foreach (EffectData data in Model.Effects) {
                if (data == null || data.Timing != EffectApplicationTiming.OnDamageDealt) continue;
                SkillEffectBase effect = SkillEffectFactory.Create(data.Effect);
                if (effect == null) continue;
                if (data.Target == EffectTargetType.DamagedEnemy) {
                    if (damaged.IsAlive) effect.Apply(Owner, data, damaged);
                } else if (data.Target == EffectTargetType.NearbyAlly) {
                    ApplyToNearbyAllies(effect, data);
                }
            }
            return applied;
        }

        private void ApplyToNearbyAllies(SkillEffectBase effect, EffectData data) {
            if (data.Radius <= 0f) return;
            HashSet<ArticleObj> seen = new();
            List<ArticleObj> allies = new();
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(Owner.transform.position, data.Radius)) {
                ArticleObj ally = collider.GetComponentInParent<ArticleObj>();
                if (ally != null && ally != Owner && ally.Model != null && seen.Add(ally))
                    allies.Add(ally);
            }
            allies.Sort((left, right) =>
                ((Vector2)left.transform.position - (Vector2)Owner.transform.position).sqrMagnitude.CompareTo(
                    ((Vector2)right.transform.position - (Vector2)Owner.transform.position).sqrMagnitude));
            int count = data.MaxTargets > 0 ? Mathf.Min(data.MaxTargets, allies.Count) : allies.Count;
            for (int i = 0; i < count; i++) effect.Apply(Owner, data, allies[i]);
        }
    }

}
