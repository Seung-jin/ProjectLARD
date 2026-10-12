using UnityEngine;

namespace LARD.Article {
    [CreateAssetMenu(fileName = "ArticleTierModel", menuName = "LARD/Article/Tier Model")]
    public sealed class ArticleTierModel : ScriptableObject {
        [Min(1)] public int Tier = 1;
        [Min(0)] public int UpgradeCost;
        [Range(0f, 1f)] public float UpgradeProbability;
        [Min(0)] public int SaleCost;
    }
}
