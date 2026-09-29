using System;
using System.Collections.Generic;
using UnityEngine;

namespace LAD.Stat {
    public sealed class StatController {
        private readonly Dictionary<StatType, float> baseStats = new Dictionary<StatType, float>();
        private readonly Dictionary<object, IReadOnlyList<StatModifier>> modifiers = new();
        private readonly Dictionary<ResourceType, float> resources = new();

        public event Action<StatType, float> OnStatChanged;
        public event Action<ResourceType, float> OnResourceChanged;

        public StatController(IReadOnlyDictionary<StatType, float> initialStats = null) {
            foreach (StatType type in Enum.GetValues(typeof(StatType))) {
                baseStats[type] = 0f;
            }

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType))) {
                resources[type] = 0f;
            }

            if (initialStats != null) {
                foreach (KeyValuePair<StatType, float> stat in initialStats) {
                    baseStats[stat.Key] = Mathf.Max(0f, stat.Value);
                }
            }

            resources[ResourceType.Health] = GetStat(StatType.MaxHealth);
        }

        public float GetBaseStat(StatType type) {
            return baseStats[type];
        }

        /// <summary>등록된 보정치를 읽기 전용으로 열거합니다. 디버그 스냅샷에 사용합니다.</summary>
        public IEnumerable<KeyValuePair<object, IReadOnlyList<StatModifier>>> EnumerateModifiers() {
            foreach (var pair in modifiers) yield return pair;
        }

        public float GetStat(StatType type) {
            float flatValue = 0f;
            float percentValue = 0f;

            foreach (IReadOnlyList<StatModifier> modifierList in modifiers.Values) {
                for (int i = 0; i < modifierList.Count; i++) {
                    StatModifier modifier = modifierList[i];
                    if (modifier.Type != type) {
                        continue;
                    }

                    flatValue += modifier.FlatValue;
                    percentValue += modifier.PercentValue;
                }
            }

            return Mathf.Max(0f, (baseStats[type] + flatValue) * (1f + percentValue));
        }

        /// <summary>개인/전역의 기본값과 보정치를 합친 후 퍼센트를 한 번 적용합니다.</summary>
        public float GetStat(StatType type, StatController additional) {
            if (additional == null || ReferenceEquals(this, additional)) return GetStat(type);
            float flat = GetBaseStat(type) + additional.GetBaseStat(type);
            float percent = 0f;
            Accumulate(type, ref flat, ref percent);
            additional.Accumulate(type, ref flat, ref percent);
            return Mathf.Max(0f, flat * (1f + percent));
        }

        private void Accumulate(StatType type, ref float flat, ref float percent) {
            foreach (var list in modifiers.Values) foreach (StatModifier modifier in list) {
                if (modifier.Type != type) continue;
                flat += modifier.FlatValue;
                percent += modifier.PercentValue;
            }
        }

        public float GetAttackInterval() {
            float attackRate = GetAttackRate();
            return attackRate > 0f ? 1f / attackRate : float.PositiveInfinity;
        }

        /// <summary>신속 100을 초당 1회로 환산한 공격 횟수입니다.</summary>
        public float GetAttackRate() {
            return GetStat(StatType.Swiftness) / 100f;
        }

        public void SetBaseStat(StatType type, float value) {
            baseStats[type] = Mathf.Max(0f, value);
            NotifyStatChanged(type);
        }

        public void AddModifiers(object source, IReadOnlyList<StatModifier> statModifiers) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            modifiers[source] = statModifiers ?? Array.Empty<StatModifier>();
            NotifyAllStatsChanged();
        }

        public bool RemoveModifiers(object source) {
            if (source == null || !modifiers.Remove(source)) {
                return false;
            }

            NotifyAllStatsChanged();
            return true;
        }

        public float GetResource(ResourceType type) {
            return resources[type];
        }

        public void SetResource(ResourceType type, float value) {
            float clampedValue = Mathf.Max(0f, value);
            if (type == ResourceType.Health) {
                clampedValue = Mathf.Min(clampedValue, GetStat(StatType.MaxHealth));
            }

            resources[type] = clampedValue;
            OnResourceChanged?.Invoke(type, clampedValue);
        }

        public void AddResource(ResourceType type, float amount) {
            SetResource(type, GetResource(type) + amount);
        }

        public bool TryConsumeResource(ResourceType type, float amount) {
            amount = Mathf.Max(0f, amount);
            if (resources[type] < amount) {
                return false;
            }

            SetResource(type, resources[type] - amount);
            return true;
        }

        public void RestoreHealthToMaximum() {
            SetResource(ResourceType.Health, GetStat(StatType.MaxHealth));
        }

        private void NotifyStatChanged(StatType type) {
            if (type == StatType.MaxHealth) {
                SetResource(ResourceType.Health, resources[ResourceType.Health]);
			} else if (type == StatType.MaxMana) {
				SetResource(ResourceType.Mana, Mathf.Min(resources[ResourceType.Mana], GetStat(StatType.MaxMana)));
            }

            OnStatChanged?.Invoke(type, GetStat(type));
        }

        private void NotifyAllStatsChanged() {
            foreach (StatType type in Enum.GetValues(typeof(StatType))) {
                NotifyStatChanged(type);
            }
        }
    }
}
