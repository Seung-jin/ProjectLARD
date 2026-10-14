using System;
using System.Collections.Generic;
using UnityEngine;

namespace LARD.Battle {
    public sealed class BattleLogEntry {
        public float Time { get; }
        public int ArticleObjectId { get; }
        public int ArticleId { get; }
        public string Race { get; }
        public int UpgradeLevel { get; }
        public float UpgradePercent { get; }
        public float BaseDamage { get; }
        public float FinalDamage { get; }
        public int MonsterObjectId { get; }
        public int MonsterId { get; }
        public float Defense { get; }
        public float HealthBefore { get; }
        public float HealthAfter { get; }

        public BattleLogEntry(float time, int articleObjectId, int articleId,
            string race, int upgradeLevel, float upgradePercent, float baseDamage, float finalDamage,
            int monsterObjectId, int monsterId, float defense, float healthBefore, float healthAfter) {
            Time = time;
            ArticleObjectId = articleObjectId;
            ArticleId = articleId;
            Race = race;
            UpgradeLevel = upgradeLevel;
            UpgradePercent = upgradePercent;
            BaseDamage = baseDamage;
            FinalDamage = finalDamage;
            MonsterObjectId = monsterObjectId;
            MonsterId = monsterId;
            Defense = defense;
            HealthBefore = healthBefore;
            HealthAfter = healthAfter;
        }
    }

    public static class BattleLog {
        private static readonly List<BattleLogEntry> EntriesInternal = new();

        public static IReadOnlyList<BattleLogEntry> Entries => EntriesInternal;
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() {
            EntriesInternal.Clear();
            Changed?.Invoke();
        }

        public static void Add(BattleLogEntry entry) {
            EntriesInternal.Add(entry);
            Changed?.Invoke();
        }

        public static void Clear() {
            EntriesInternal.Clear();
            Changed?.Invoke();
        }
    }
}
