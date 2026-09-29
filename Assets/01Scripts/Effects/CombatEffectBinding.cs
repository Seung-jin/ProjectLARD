using System;
using System.Collections.Generic;
using LAD.Article;
using LAD.Battle;
using LAD.Skill;
using LAD.Stat;
using UnityEngine;

namespace LAD.Effects {
    /// <summary>한 기물의 옵저버에 전투 효과 하나를 연결합니다. 각인 계열 ID에 의존하지 않습니다.</summary>
    public sealed class CombatEffectBinding : IDisposable {
        private readonly ArticleObj owner;
        private readonly EffectType type;
        private readonly float value;
        private readonly HashSet<EffectController> targets = new HashSet<EffectController>();
        private readonly BattleObjEventType eventType;
        private int count;
        private readonly HashSet<long> countedAttacks = new HashSet<long>();
        private bool disposed;

        public CombatEffectBinding(ArticleObj owner, EffectType type, float value) {
            this.owner = owner;
            this.type = type;
            this.value = value;
            eventType = type == EffectType.ManaFlow ? BattleObjEventType.SkillActivated :
                type == EffectType.ConditionalDamageIncrease ? BattleObjEventType.BeforeDamage : BattleObjEventType.DamageDealt;
            owner.Observer.AddListener(eventType, OnEvent);
        }

        private void OnEvent(params object[] args) {
            if (disposed || owner == null || owner.IsDead || !owner.IsOnBattlefield) return;
            if (type == EffectType.ManaFlow) {
                count = Mathf.Min(6, count + 1);
                owner.StatController.AddModifiers(this,
                    new[] { new StatModifier(StatType.ManaRegen, 0f, count * value / 100f) });
                return;
            }
            if (args.Length == 0 || args[0] is not AttackParameters attack || attack.Attacker != owner) return;
            if (type == EffectType.ConditionalDamageIncrease) {
                if (attack.IsDirectDamage && attack.Target is LAD.Monster.MonsterObj monster &&
                    monster.BuffController.HasCrowdControl(CrowdControlType.Slow | CrowdControlType.Stun | CrowdControlType.Root))
                    attack.DamageMultiplier *= 1f + value;
                return;
            }
            if (attack.Damage <= 0f) return;
            // 적중 횟수는 투사체 생성 시점이 아니라 실제 기본 공격 피해가 발생했을 때 셉니다.
            if (type == EffectType.LightningStrike && attack.Category == AttackCategory.BasicAttack) {
                if (attack.AttackId != 0 && !countedAttacks.Add(attack.AttackId)) return;
                if (++count < 5) return;
                count = 0;
                if (attack.IsTargetAlive && attack.Target is LAD.Monster.MonsterObj lightningTarget)
                    BattleLogic.ApplyDamage(owner.BasicAttackDamage * value, owner, lightningTarget,
                        DamageType.Magic, "번개의 분노", AttackCategory.Effect);
                return;
            }
            if (!attack.IsTargetAlive || attack.Target is not LAD.Monster.MonsterObj target) return;
            float duration;
            float amount = value;
            switch (type) {
                case EffectType.DamageTakenIncrease:
                    if (attack.Category != AttackCategory.Skill) return;
                    duration = 4f;
                    break;
                case EffectType.MoveSpeedReduction:
                    if (attack.Category != AttackCategory.BasicAttack) return;
                    duration = 2f;
                    break;
                case EffectType.DamageReductionPoints:
                    if (attack.Category != AttackCategory.BasicAttack) return;
                    duration = 3f;
                    break;
                case EffectType.PoisonDamage:
                    if (attack.Category != AttackCategory.Skill) return;
                    duration = 4f;
                    amount = owner.BasicAttackDamage * value;
                    break;
                default:
                    return;
            }
            EffectData data = EffectData.CreateRuntime(type, amount, duration, type.ToString(),
                type == EffectType.PoisonDamage ? EffectStackPolicy.StrongestOnly : EffectStackPolicy.Single,
                EffectDurationPolicy.Refresh, EffectStackScope.SharedOnTarget);
            if (target.Effects.Apply(data, owner, this)) targets.Add(target.Effects);
        }

        public void Dispose() {
            if (disposed) return;
            disposed = true;
            if (owner != null) {
                owner.Observer.RemoveListener(eventType, OnEvent);
                owner.StatController?.RemoveModifiers(this);
            }
            foreach (EffectController target in targets) target.RemoveSource(this);
            targets.Clear();
            countedAttacks.Clear();
        }
    }
}
