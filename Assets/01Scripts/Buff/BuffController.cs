using System;
using System.Collections.Generic;
using LAD.Skill;
using LAD.Stat;

namespace LAD.Buff {
    public sealed class BuffController {
        private sealed class ActiveBuff {
            public Buff Buff { get; set; }
            public string GroupId => Buff.Id;
            public float RemainingDuration { get; set; }

            public ActiveBuff(Buff buff) {
                Buff = buff;
                RemainingDuration = ToRemainingDuration(buff.Duration);
            }
        }

        private readonly StatController statController;
        private readonly Dictionary<string, ActiveBuff> activeBuffs = new Dictionary<string, ActiveBuff>();
        private readonly List<string> expiredBuffIds = new List<string>();
        private int stackSequence;

        public event Action<Buff> OnBuffAdded;
        public event Action<Buff> OnBuffRemoved;

        public BuffController(StatController statController) {
            this.statController = statController ?? throw new ArgumentNullException(nameof(statController));
        }

        public bool HasBuff(string id) {
            if (string.IsNullOrEmpty(id)) {
                return false;
            }

            foreach (ActiveBuff activeBuff in activeBuffs.Values) {
                if (activeBuff.GroupId == id) {
                    return true;
                }
            }
            return false;
        }

        public bool HasDebuff() {
            foreach (ActiveBuff activeBuff in activeBuffs.Values) {
                if (activeBuff.Buff.Type == BuffType.Debuff) return true;
            }
            return false;
        }

        public bool HasCrowdControl(LAD.Effects.CrowdControlType types) {
            foreach (ActiveBuff active in activeBuffs.Values) {
                if ((active.Buff.CrowdControl & types) != 0) return true;
                // 기존 스킬의 이동 속도 감소 버프도 둔화로 인식합니다.
                if ((types & LAD.Effects.CrowdControlType.Slow) == 0) continue;
                foreach (StatModifier modifier in active.Buff.StatModifiers) {
                    if (modifier.Type == StatType.MoveSpeed &&
                        (modifier.FlatValue < 0f || modifier.PercentValue < 0f)) return true;
                }
            }
            return false;
        }

        public void AddBuff(Buff buff) {
            if (buff == null) {
                throw new ArgumentNullException(nameof(buff));
            }

            if (string.IsNullOrEmpty(buff.Id)) {
                throw new ArgumentException("버프 ID는 비어 있을 수 없습니다.", nameof(buff));
            }

            RemoveBuff(buff.Id);
            AddActiveBuff(buff.Id, buff);
        }

        public void AddBuff(Buff buff, EffectStackPolicy stackPolicy,
                            EffectDurationPolicy durationPolicy, int maxStacks) {
            ValidateBuff(buff);

            List<KeyValuePair<string, ActiveBuff>> group = FindGroup(buff.Id);
            switch (stackPolicy) {
                case EffectStackPolicy.Independent:
                case EffectStackPolicy.Unlimited:
                    AddStack(buff);
                    return;
                case EffectStackPolicy.Limited:
                    if (group.Count < Math.Max(1, maxStacks)) {
                        AddStack(buff);
                    }
                    else {
                        ApplyDurationPolicy(group[0].Value, buff.Duration, durationPolicy);
                    }
                    return;
                case EffectStackPolicy.StrongestOnly:
                    ApplyStrongestOnly(group, buff, durationPolicy);
                    return;
                case EffectStackPolicy.Single:
                default:
                    ApplySingle(group, buff, durationPolicy);
                    return;
            }
        }

        public bool RemoveBuff(string id) {
            if (string.IsNullOrEmpty(id)) {
                return false;
            }

            List<KeyValuePair<string, ActiveBuff>> group = FindGroup(id);
            for (int i = 0; i < group.Count; i++) {
                RemoveActiveBuff(group[i].Key);
            }
            return group.Count > 0;
        }

        public void Update(float deltaTime) {
            if (deltaTime <= 0f) {
                return;
            }

            expiredBuffIds.Clear();
            foreach (KeyValuePair<string, ActiveBuff> pair in activeBuffs) {
                ActiveBuff activeBuff = pair.Value;
                if (float.IsPositiveInfinity(activeBuff.RemainingDuration)) {
                    continue;
                }

                activeBuff.RemainingDuration -= deltaTime;
                if (activeBuff.RemainingDuration <= 0f) {
                    expiredBuffIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < expiredBuffIds.Count; i++) {
                RemoveActiveBuff(expiredBuffIds[i]);
            }
        }

        public void Clear() {
            expiredBuffIds.Clear();
            expiredBuffIds.AddRange(activeBuffs.Keys);

            for (int i = 0; i < expiredBuffIds.Count; i++) {
                RemoveActiveBuff(expiredBuffIds[i]);
            }

            expiredBuffIds.Clear();
        }

        private void ApplySingle(List<KeyValuePair<string, ActiveBuff>> group, Buff buff,
                                 EffectDurationPolicy durationPolicy) {
            if (group.Count == 0) {
                AddActiveBuff(buff.Id, buff);
                return;
            }

            ActiveBuff current = group[0].Value;
            ApplyDurationPolicy(current, buff.Duration, durationPolicy);
            statController.RemoveModifiers(current);
            current.Buff = buff;
            statController.AddModifiers(current, buff.StatModifiers);
        }

        private void ApplyStrongestOnly(List<KeyValuePair<string, ActiveBuff>> group, Buff buff,
                                        EffectDurationPolicy durationPolicy) {
            if (group.Count == 0) {
                AddActiveBuff(buff.Id, buff);
                return;
            }

            KeyValuePair<string, ActiveBuff> current = group[0];
            if (GetModifierStrength(buff) > GetModifierStrength(current.Value.Buff)) {
                RemoveActiveBuff(current.Key);
                AddActiveBuff(buff.Id, buff);
                return;
            }

            ApplyDurationPolicy(current.Value, buff.Duration, durationPolicy);
        }

        private static void ApplyDurationPolicy(ActiveBuff activeBuff, float duration,
                                                EffectDurationPolicy durationPolicy) {
            switch (durationPolicy) {
                case EffectDurationPolicy.Keep:
                case EffectDurationPolicy.Independent:
                    return;
                case EffectDurationPolicy.Extend:
                    float addedDuration = ToRemainingDuration(duration);
                    activeBuff.RemainingDuration =
                        float.IsPositiveInfinity(addedDuration)
                            ? float.PositiveInfinity
                            : activeBuff.RemainingDuration + addedDuration;
                    return;
                case EffectDurationPolicy.Refresh:
                default:
                    activeBuff.RemainingDuration = ToRemainingDuration(duration);
                    return;
            }
        }

        private static float ToRemainingDuration(float duration) {
            return duration <= 0f ? float.PositiveInfinity : duration;
        }

        private static float GetModifierStrength(Buff buff) {
            float strength = 0f;
            for (int i = 0; i < buff.StatModifiers.Count; i++) {
                StatModifier modifier = buff.StatModifiers[i];
                strength += Math.Abs(modifier.FlatValue) + Math.Abs(modifier.PercentValue);
            }
            return strength;
        }

        private void AddStack(Buff buff) {
            AddActiveBuff($"{buff.Id}.Stack{++stackSequence}", buff);
        }

        private void AddActiveBuff(string key, Buff buff) {
            ActiveBuff activeBuff = new ActiveBuff(buff);
            activeBuffs.Add(key, activeBuff);
            statController.AddModifiers(activeBuff, buff.StatModifiers);
            OnBuffAdded?.Invoke(buff);
        }

        private bool RemoveActiveBuff(string key) {
            if (!activeBuffs.TryGetValue(key, out ActiveBuff activeBuff)) {
                return false;
            }

            activeBuffs.Remove(key);
            statController.RemoveModifiers(activeBuff);
            OnBuffRemoved?.Invoke(activeBuff.Buff);
            return true;
        }

        private List<KeyValuePair<string, ActiveBuff>> FindGroup(string groupId) {
            var result = new List<KeyValuePair<string, ActiveBuff>>();
            foreach (KeyValuePair<string, ActiveBuff> pair in activeBuffs) {
                if (pair.Value.GroupId == groupId) {
                    result.Add(pair);
                }
            }
            return result;
        }

        private static void ValidateBuff(Buff buff) {
            if (buff == null) {
                throw new ArgumentNullException(nameof(buff));
            }
            if (string.IsNullOrEmpty(buff.Id)) {
                throw new ArgumentException("버프 ID는 비어 있을 수 없습니다.", nameof(buff));
            }
        }
    }
}
