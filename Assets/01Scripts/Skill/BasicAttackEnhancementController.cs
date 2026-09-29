using System;
using System.Collections.Generic;
using LAD.Battle;

namespace LAD.Skill {
    /// <summary>기본 공격 적중에만 적용되는 시간제 강화. 같은 키로 재시전하면 갱신한다.</summary>
    public sealed class BasicAttackEnhancementController {
        private sealed class Entry {
            public float Remaining;
            public Action<IDamageable> OnHit;
        }
        private readonly Dictionary<string, Entry> entries = new();
        private readonly List<string> expiredKeys = new();
        public void Set(string key, float duration, Action<IDamageable> onHit) {
            if (duration > 0f && onHit != null)
                entries[key] = new Entry { Remaining = duration, OnHit = onHit };
        }
        public void Remove(string key) => entries.Remove(key);
        public void Clear() => entries.Clear();
        public void Update(float deltaTime) {
            if (entries.Count == 0 || deltaTime <= 0f) return;
            expiredKeys.Clear();
            foreach (var pair in entries) {
                pair.Value.Remaining -= deltaTime;
                if (pair.Value.Remaining <= 0f) expiredKeys.Add(pair.Key);
            }
            foreach (string key in expiredKeys) entries.Remove(key);
        }
        public void ApplyHit(IDamageable target) {
            if (entries.Count == 0) return;
            foreach (var entry in new List<Entry>(entries.Values))
                if (entry.Remaining > 0f) entry.OnHit(target);
        }
    }
}
