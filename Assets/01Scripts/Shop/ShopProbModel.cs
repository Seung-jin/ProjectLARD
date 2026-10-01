using System;
using System.Collections.Generic;
using UnityEngine;

namespace LARD.Shop {
    [Serializable]
    public struct TierWeight {
        [Min(1)] public int Tier;
        [Min(0)] public int Weight;
    }

    [CreateAssetMenu(fileName = "ShopProbModel", menuName = "Data/Shop/Tier Probabilities")]
    public sealed class ShopProbModel : ScriptableObject {
        public int Id;
        public List<TierWeight> TierWeights = new();

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
