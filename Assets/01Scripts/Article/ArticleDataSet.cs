using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LAD.Data;
using LSJ.Util;
using UnityEngine;

namespace LAD.Article {
    public sealed class ArticleDataSet : IDataSet {
        public const string AddressableLabel = "Article";

        private static readonly Dictionary<int, ArticleModel> models = new Dictionary<int, ArticleModel>();
        private static readonly Dictionary<int, List<int>> idsByTier = new Dictionary<int, List<int>>();
		public static IReadOnlyCollection<ArticleModel> Models => models.Values;

        public async UniTask Load() {
            models.Clear();
            idsByTier.Clear();

            IList<ScriptableObject> assets = await AddressableManager.LoadAssetAsyncList<ScriptableObject>(
                new[] { AddressableLabel },
                AddAsset);

            if (assets == null) {
                Debug.LogError($"ArticleDataSet 로드에 실패했습니다. Addressable Label: {AddressableLabel}");
            }
        }

        public static bool TryGet(int id, out ArticleModel model) {
            return models.TryGetValue(id, out model);
        }

        public static ArticleModel Get(int id) {
            if (TryGet(id, out ArticleModel model)) {
                return model;
            }

            Debug.LogError($"ID가 {id}인 ArticleModel을 찾을 수 없습니다.");
            return null;
        }

		public static string GetName(int id) {
			return $"Article_{id}";
		}

		public static bool TryGetRandomId(int tier, out int id) {
			id = -1;
			if (!idsByTier.TryGetValue(tier, out List<int> ids) || ids.Count == 0) {
				return false;
			}

			id = ids[Random.Range(0, ids.Count)];
			return true;
		}

        private static void AddAsset(ScriptableObject asset) {
            if (asset is not ArticleModel articleModel) {
                return;
            }

            if (!models.TryAdd(articleModel.Id, articleModel)) {
                Debug.LogError($"ArticleModel ID가 중복되었습니다: {articleModel.Id} ({articleModel.name})");
				return;
            }

			if (!idsByTier.TryGetValue(articleModel.Tier, out List<int> ids)) {
				ids = new List<int>();
				idsByTier.Add(articleModel.Tier, ids);
			}

			ids.Add(articleModel.Id);
        }
    }
}
