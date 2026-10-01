using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Data;
using LSJ.Util;
using UnityEngine;

namespace LARD.Shop {
	public sealed class ShopDataSet : IDataSet {
		public const string AddressableLabel = "Shop";

		private static readonly Dictionary<int, ShopConstModel> _constants = new();
		private static readonly Dictionary<int, ShopProbModel> _probabilities = new();

		public async UniTask Load() {
			_constants.Clear();
			_probabilities.Clear();

			IList<ScriptableObject> assets = await AddressableManager.LoadAssetAsyncList<ScriptableObject>(
			                                                                                               new[] { AddressableLabel }, AddAsset);

			if (assets == null) {
				Debug.LogError($"ShopDataSet load failed. Addressable label: {AddressableLabel}");
			}
		}

		public static ShopConstModel GetConstModel(int id) => _constants.GetValueOrDefault(id);
		public static ShopProbModel GetProbModel(int id) => _probabilities.GetValueOrDefault(id);

		private static void AddAsset(ScriptableObject asset) {
			switch (asset) {
				case ShopConstModel constants:
					if (!_constants.TryAdd(constants.Id, constants)) {
						Debug.LogError($"Duplicate ShopConstModel ID: {constants.Id} ({constants.name})");
					}

					break;
				case ShopProbModel probabilities:
					if (!_probabilities.TryAdd(probabilities.Id, probabilities)) {
						Debug.LogError($"Duplicate ShopProbModel ID: {probabilities.Id} ({probabilities.name})");
					}

					break;
			}
		}
	}
}