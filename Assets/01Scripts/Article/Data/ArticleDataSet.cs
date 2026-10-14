using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Data;
using LSJ.Util;
using UnityEngine;

namespace LARD.Article {
	public sealed class ArticleDataSet : IDataSet {
		public const string AddressableLabel = "Article";

		private static readonly Dictionary<int, ArticleModel> _models = new();
		private static readonly Dictionary<int, ArticleTierModel> _tierModels = new();
		private static readonly Dictionary<int, List<ArticleModel>> _modelsByTier = new();
		private static readonly Dictionary<(ArticleRace race, int tier), List<ArticleModel>> _modelsByRaceAndTier = new();

		public async UniTask Load() {
			_models.Clear();
			_tierModels.Clear();
			_modelsByTier.Clear();
			_modelsByRaceAndTier.Clear();

			IList<ScriptableObject> assets = await AddressableManager.LoadAssetAsyncList<ScriptableObject>(
			                                                                                               new[] { AddressableLabel },
			                                                                                               AddAsset);

			if (assets == null) {
				Debug.LogError($"ArticleDataSet 로드에 실패했습니다. Addressable Label: {AddressableLabel}");
				return;
			}

			foreach (ArticleModel model in _models.Values) {
				if (model.NextTierModelId > 0 && !TryGetNextTierModel(model, model.Tier + 1, out _)) {
					Debug.LogError($"ArticleModel의 다음 티어 연결이 올바르지 않습니다: {model.Id} -> {model.NextTierModelId} ({model.name})");
				}
			}
		}

		public static ArticleModel GetModel(int id) => _models.GetValueOrDefault(id);
		public static ArticleTierModel GetTierModel(int tier) => _tierModels.GetValueOrDefault(tier);
		public static bool TryPickArticle(int tier, out ArticleModel model) {
			model = null;
			if (!_tierModels.ContainsKey(tier) || !_modelsByTier.TryGetValue(tier, out List<ArticleModel> models) || models.Count == 0) {
				return false;
			}

			model = models[Random.Range(0, models.Count)];
			return true;
		}

		public static bool HasArticle(ArticleRace race, int tier) =>
			_modelsByRaceAndTier.TryGetValue((race, tier), out List<ArticleModel> models) && models.Count > 0;

		public static bool TryPickArticle(ArticleRace race, int tier, out ArticleModel model) {
			model = null;
			if (!_tierModels.ContainsKey(tier) || !HasArticle(race, tier)) {
				return false;
			}

			List<ArticleModel> models = _modelsByRaceAndTier[(race, tier)];
			model = models[Random.Range(0, models.Count)];
			return true;
		}

		public static bool TryGetNextTierModel(ArticleModel current, int nextTier, out ArticleModel model) {
			model = current != null && current.NextTierModelId > 0
				? GetModel(current.NextTierModelId) : null;
			if (model == null || model.Race != current.Race || model.Tier != nextTier || model.Prefab == null) {
				model = null;
				return false;
			}

			return true;
		}

		private static void AddAsset(ScriptableObject asset) {
			if (asset is ArticleTierModel tierModel) {
				if (!_tierModels.TryAdd(tierModel.Tier, tierModel)) {
					Debug.LogError($"ArticleTierModel이 중복되었습니다: tier {tierModel.Tier} ({tierModel.name})");
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

			if (articleModel.Prefab == null) {
				Debug.LogError($"ArticleModel에 Prefab이 없습니다: {articleModel.name}");
				return;
			}

			if (!_modelsByTier.TryGetValue(articleModel.Tier, out List<ArticleModel> models)) {
				models = new List<ArticleModel>();
				_modelsByTier.Add(articleModel.Tier, models);
			}
			models.Add(articleModel);

			var key = (articleModel.Race, articleModel.Tier);
			if (!_modelsByRaceAndTier.TryGetValue(key, out List<ArticleModel> raceModels)) {
				raceModels = new List<ArticleModel>();
				_modelsByRaceAndTier.Add(key, raceModels);
			}
			raceModels.Add(articleModel);
		}
	}
}
