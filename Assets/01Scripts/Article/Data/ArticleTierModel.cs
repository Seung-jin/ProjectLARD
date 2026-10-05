using UnityEngine;

namespace LARD.Article {
    [CreateAssetMenu(fileName = "ArticleTierModel", menuName = "LARD/Article/Tier Model")]
    public sealed class ArticleTierModel : ScriptableObject {
        [SerializeField] private ArticleModel _article;
        [SerializeField, Min(1)] private int _tier = 1;
        [SerializeField] private ArticleTierModel _nextTier;
        [SerializeField, Min(0)] private int _upgradeCost;
        [SerializeField, Range(0f, 1f)] private float _upgradeProbability;
        [SerializeField, Min(0)] private int _saleCost;
        [SerializeField, Min(0)] private int _drawWeight;

        public ArticleModel Article => _article;
        public int Tier => _tier;
        public ArticleTierModel NextTier => _nextTier;
        public int UpgradeCost => _upgradeCost;
        public float UpgradeProbability => _upgradeProbability;
        public int SaleCost => _saleCost;
        public int DrawWeight => _drawWeight;
    }
}
