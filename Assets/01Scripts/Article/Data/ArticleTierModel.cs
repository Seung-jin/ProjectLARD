using UnityEngine;

namespace LARD.Article {
    [CreateAssetMenu(fileName = "ArticleTierModel", menuName = "LARD/Article/Tier Model")]
    public sealed class ArticleTierModel : ScriptableObject {
        [SerializeField] private ArticleModel _article;
        [SerializeField, Min(1)] private int _tier = 1;
        [SerializeField, Min(0)] private int _upgradeCost;
        [SerializeField, Min(0)] private int _saleCost;

        public ArticleModel Article => _article;
        public int Tier => _tier;
        public int UpgradeCost => _upgradeCost;
        public int SaleCost => _saleCost;
    }
}
