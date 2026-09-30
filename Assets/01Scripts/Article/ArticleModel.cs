using UnityEngine;

namespace LARD.Article {
    [CreateAssetMenu(fileName = "ArticleModel", menuName = "LARD/Article Model")]
    public sealed class ArticleModel : ScriptableObject {
        [SerializeField] private ArticleObj _prefab;

        public ArticleObj Prefab => _prefab;
    }

}
