using System;
using System.Collections.Generic;
using LSJ.Util;
using UnityEngine;

namespace LARD.Shop {
    [Serializable]
    public struct TierWeight {
        [Min(1)] public int Tier;
        [Min(0)] public int Weight;
    }

    [CreateAssetMenu(fileName = "ShopProbModel", menuName = "Data/Shop/Tier Probabilities")]
    public sealed class ShopProbModel : ScriptableObject {
        private sealed class TierDrawEntry {
            public readonly int Tier;
            public readonly int Weight;

            public TierDrawEntry(int tier, int weight) {
                Tier = tier;
                Weight = weight;
            }
        }

        public int Id;
        public List<TierWeight> TierWeights = new();
        private ProbTable<TierDrawEntry> _drawTable;

        internal void BuildDrawTable() {
            _drawTable = null;
            if (TierWeights == null) {
                Debug.LogError($"[ShopProbModel] Tier weights are missing: {name}");
                return;
            }

            var table = new ProbTable<TierDrawEntry>(entry => entry.Weight);
            var seenTiers = new HashSet<int>();
            long totalWeight = 0;
            foreach (TierWeight entry in TierWeights) {
                if (entry.Tier < 1 || entry.Weight < 0 || !seenTiers.Add(entry.Tier)) {
                    Debug.LogError($"[ShopProbModel] Invalid or duplicate tier weight: tier {entry.Tier}, weight {entry.Weight} ({name})");
                    return;
                }

                if (entry.Weight == 0) {
                    continue;
                }

                totalWeight += entry.Weight;
                if (totalWeight > int.MaxValue) {
                    Debug.LogError($"[ShopProbModel] Total tier weight exceeds int.MaxValue: {name}");
                    return;
                }

                table.Add(new TierDrawEntry(entry.Tier, entry.Weight));
            }

            if (table.Count == 0) {
                Debug.LogError($"[ShopProbModel] No tier has a positive draw weight: {name}");
                return;
            }

            _drawTable = table;
        }

        public bool TryPickTier(out int tier) {
            tier = 0;
            if (_drawTable == null) {
                return false;
            }

            tier = _drawTable.Pick().Tier;
            return true;
        }

        public bool TryGetWeight(int tier, out int weight) {
            foreach (TierWeight entry in TierWeights) {
                if (entry.Tier == tier) {
                    weight = entry.Weight;
                    return true;
                }
            }

            weight = 0;
            return false;
        }
    }
}
