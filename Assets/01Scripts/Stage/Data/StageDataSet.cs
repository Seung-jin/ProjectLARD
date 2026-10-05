using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Data;
using LSJ.Util;
using UnityEngine;

namespace LARD.Stage {
    public sealed class StageDataSet : IDataSet {
        public const string AddressableLabel = "Stage";

        private static readonly Dictionary<int, StageModel> _models = new();

        public async UniTask Load() {
            _models.Clear();

            IList<StageModel> assets = await AddressableManager.LoadAssetAsyncList<StageModel>(
                new[] { AddressableLabel }, AddAsset);

            if (assets == null) {
                Debug.LogError($"StageDataSet load failed. Addressable label: {AddressableLabel}");
            }
        }

        public static StageModel GetModel(int id) => _models.GetValueOrDefault(id);

        private static void AddAsset(StageModel model) {
            if (!_models.TryAdd(model.Id, model)) {
                Debug.LogError($"Duplicate StageModel ID: {model.Id} ({model.name})");
            }
        }
    }
}
