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

            IList<ScriptableObject> assets = await AddressableManager.LoadAssetAsyncList<ScriptableObject>(
                new[] { AddressableLabel }, AddAsset);

            if (assets == null) {
                Debug.LogError($"StageDataSet load failed. Addressable label: {AddressableLabel}");
            }
        }

        public static StageModel GetModel(int id) => _models.GetValueOrDefault(id);

        private static void AddAsset(ScriptableObject asset) {
            if (asset is StageModel model && !_models.TryAdd(model.Id, model)) {
                Debug.LogError($"Duplicate StageModel ID: {model.Id} ({model.name})");
            }
        }
    }
}
