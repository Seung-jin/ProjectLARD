using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Data;
using LSJ.Util;
using UnityEngine;

namespace LARD.Article {
	public sealed class ArticleDataSet : IDataSet {
		public const string AddressableLabel = "Article";

		private static Dictionary<int, ArticleModel> _models = new();

		public async UniTask Load() {
			_models.Clear();

			IList<ScriptableObject> assets = await AddressableManager.LoadAssetAsyncList<ScriptableObject>(
			                                                                                               new[] { AddressableLabel },
			                                                                                               AddAsset);

			if (assets == null) {
				Debug.LogError($"ArticleDataSet 로드에 실패했습니다. Addressable Label: {AddressableLabel}");
			}
		}

		public static ArticleModel GetModel(int id) => _models.GetValueOrDefault(id);

		private static void AddAsset(ScriptableObject asset) {
			if (asset is not ArticleModel articleModel) {
				return;
			}

			if (!_models.TryAdd(articleModel.Id, articleModel)) {
				Debug.LogError($"ArticleModel ID가 중복되었습니다: {articleModel.Id} ({articleModel.name})");
				return;
			}
		}
	}
}