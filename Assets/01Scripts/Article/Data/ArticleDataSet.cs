using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Data;
using LSJ.Util;
using UnityEngine;

namespace LARD.Article {
	public sealed class ArticleDataSet : IDataSet {
		public const string AddressableLabel = "Article";

		private static Dictionary<int, ArticleModel> _models = new();
		private static readonly List<ArticleTierModel> _tierModels = new();
		private static readonly Dictionary<(int articleId, int tier), ArticleTierModel> _tierModelsByArticle = new();
		private static readonly Dictionary<int, ProbTable<ArticleTierModel>> _drawTablesByTier = new();

		public async UniTask Load() {
			_models.Clear();
			_tierModels.Clear();
			_tierModelsByArticle.Clear();
			_drawTablesByTier.Clear();

			IList<ScriptableObject> assets = await AddressableManager.LoadAssetAsyncList<ScriptableObject>(
			                                                                                               new[] { AddressableLabel },
			                                                                                               AddAsset);

			if (assets == null) {
				Debug.LogError($"ArticleDataSet 로드에 실패했습니다. Addressable Label: {AddressableLabel}");
				return;
			}

			foreach (ArticleTierModel tier in _tierModels) {
				if (tier.DrawWeight <= 0) {
					continue;
				}

				if (tier.Article == null || tier.Article.Prefab == null ||
				    !_models.TryGetValue(tier.Article.Id, out ArticleModel model) || model != tier.Article) {
					Debug.LogError($"추첨 가능한 ArticleTierModel의 기물 연결이 올바르지 않습니다: {tier.name}");
					continue;
				}

				if (!_drawTablesByTier.TryGetValue(tier.Tier, out ProbTable<ArticleTierModel> drawTable)) {
					drawTable = new ProbTable<ArticleTierModel>(item => item.DrawWeight);
					_drawTablesByTier.Add(tier.Tier, drawTable);
				}
				drawTable.Add(tier);
			}
		}

		public static ArticleModel GetModel(int id) => _models.GetValueOrDefault(id);
		public static ArticleTierModel GetTierModel(int articleId, int tier) =>
			_tierModelsByArticle.GetValueOrDefault((articleId, tier));
		public static bool TryPickArticleTier(int tierNumber, out ArticleTierModel tier) {
			tier = _drawTablesByTier.TryGetValue(tierNumber, out ProbTable<ArticleTierModel> drawTable) &&
			       drawTable.Count > 0 ? drawTable.Pick() : null;
			return tier != null;
		}

		private static void AddAsset(ScriptableObject asset) {
			if (asset is ArticleTierModel tierModel) {
				_tierModels.Add(tierModel);
				if (tierModel.Article != null && !_tierModelsByArticle.TryAdd((tierModel.Article.Id, tierModel.Tier), tierModel)) {
					Debug.LogError($"ArticleTierModel이 중복되었습니다: {tierModel.Article.Id}, tier {tierModel.Tier} ({tierModel.name})");
				}
				return;
			}

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
