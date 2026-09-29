using System;
using System.Collections.Generic;
using LAD.Battle;
using LAD.Buff;
using LAD.Skill;
using LAD.Stat;
using UnityEngine;

namespace LAD.Effects {
    /// <summary>대상에 붙은 효과의 수명과 지속 피해를 관리합니다. Tick은 이 대상의 옵저버만 사용합니다.</summary>
    public sealed class EffectController : IDisposable {
        private sealed class Poison {
            public object SourceKey;
            public BattleObj Source;
            public int SourceLife;
            public float Damage;
            public float Remaining;
            public float UntilTick = 1f;
        }

        private readonly BattleObj owner;
        private readonly BattleManager manager;
        private readonly Dictionary<string, object> buffSources = new Dictionary<string, object>();
        private readonly Dictionary<string, Poison> poisons = new Dictionary<string, Poison>();
        private bool disposed;

        public EffectController(BattleObj owner) {
            this.owner = owner;
            manager = BattleManager.Current;
            manager?.Observer.AddListener(BattleManagerEventType.RoundEnded, OnRoundEnded);
            owner.Observer.AddListener(BattleObjEventType.Tick, OnTick);
        }

        public bool Apply(EffectData data, BattleObj source, object sourceKey) {
            if (disposed || owner == null || owner.IsDead || !data.MeetsCondition(owner)) return false;
            string key = $"Effect.{data.StackKey}";
            if (data.StackScope == EffectStackScope.PerSource && source != null)
                key += $".{source.GetEntityId()}.{source.LifeVersion}";
            if (data.Effect == EffectType.PoisonDamage) {
                // Value는 시전자의 기본 공격 계수까지 반영한, 치명타 이전의 틱 원본 피해입니다.
                if (!poisons.TryGetValue(key, out Poison poison)) {
                    poison = new Poison();
                    poisons.Add(key, poison);
                }
                if (data.Value >= poison.Damage) {
                    poison.SourceKey = sourceKey;
                    poison.Source = source;
                    poison.SourceLife = source != null ? source.LifeVersion : 0;
                    poison.Damage = data.Value;
                }
                // 재적중은 수명만 갱신하여 잦은 공격으로 틱이 무한히 밀리지 않게 합니다.
                poison.Remaining = data.Duration;
                return true;
            }

            StatModifier modifier;
            CrowdControlType status = CrowdControlType.None;
            switch (data.Effect) {
                case EffectType.MoveSpeedReduction:
                    modifier = new StatModifier(StatType.MoveSpeed, 0f, -Mathf.Clamp01(data.Value));
                    status = CrowdControlType.Slow;
                    break;
                case EffectType.DamageReductionPoints:
                    modifier = new StatModifier(StatType.DamageReduction, -data.Value);
                    break;
                case EffectType.DamageTakenIncrease:
                    modifier = new StatModifier(StatType.DamageTakenIncrease, data.Value);
                    break;
                case EffectType.Stun:
                case EffectType.Root:
                    modifier = new StatModifier(StatType.MoveSpeed, 0f, -1f);
                    status = data.Effect == EffectType.Stun ? CrowdControlType.Stun : CrowdControlType.Root;
                    break;
                default:
                    return false;
            }
            owner.BuffController.AddBuff(new LAD.Buff.Buff(key, BuffType.Debuff, data.Duration,
                new[] { modifier }, status), data.StackPolicy, data.DurationPolicy, data.MaxStacks);
            buffSources[key] = sourceKey;
            return true;
        }

        public void RemoveSource(object sourceKey) {
            foreach (var pair in new List<KeyValuePair<string, object>>(buffSources)) {
                if (!ReferenceEquals(pair.Value, sourceKey)) continue;
                owner.BuffController?.RemoveBuff(pair.Key);
                buffSources.Remove(pair.Key);
            }
            foreach (var pair in new List<KeyValuePair<string, Poison>>(poisons)) {
                if (ReferenceEquals(pair.Value.SourceKey, sourceKey)) poisons.Remove(pair.Key);
            }
        }

        private void OnTick(params object[] args) {
            if (disposed || args.Length < 2 || args[1] is not float deltaTime || deltaTime <= 0f) return;
            foreach (var pair in new List<KeyValuePair<string, Poison>>(poisons)) {
                Poison poison = pair.Value;
                if (!poisons.ContainsKey(pair.Key)) continue;
                float elapsed = Mathf.Min(deltaTime, poison.Remaining);
                poison.Remaining -= elapsed;
                poison.UntilTick -= elapsed;
                while (poison.UntilTick <= 0.00001f && !disposed && !owner.IsDead && poisons.ContainsKey(pair.Key)) {
                    poison.UntilTick += 1f;
                    BattleObj source = poison.Source;
                    if (source == null || source.LifeVersion != poison.SourceLife) {
                        poisons.Remove(pair.Key);
                        break;
                    }
                    BattleLogic.ApplyDamage(poison.Damage, source, owner, DamageType.Magic,
                        "중독", AttackCategory.Periodic);
                }
                if (poison.Remaining <= 0f) poisons.Remove(pair.Key);
            }
        }

        public void Clear() {
            foreach (string key in new List<string>(buffSources.Keys)) owner.BuffController?.RemoveBuff(key);
            buffSources.Clear();
            poisons.Clear();
        }

        private void OnRoundEnded(params object[] args) => Clear();

        public void Dispose() {
            if (disposed) return;
            disposed = true;
            owner.Observer.RemoveListener(BattleObjEventType.Tick, OnTick);
            if (manager != null) manager.Observer.RemoveListener(BattleManagerEventType.RoundEnded, OnRoundEnded);
            Clear();
        }
    }
}
