using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Data;
using LSJ.Util;
using UnityEngine;

namespace LARD.Stage {
    public sealed class StageDataSet : IDataSet {
        public const string AddressableLabel = "Stage";

        private static readonly Dictionary<int, StageModel> _models = new();
        private static StageConstModel _constants;

        public async UniTask Load() {
            _models.Clear();
            _constants = null;

            IList<ScriptableObject> assets = await AddressableManager.LoadAssetAsyncList<ScriptableObject>(
                new[] { AddressableLabel }, AddAsset);

            if (assets == null) {
                Debug.LogError($"StageDataSet load failed. Addressable label: {AddressableLabel}");
                return;
            }
            if (_constants == null) {
                Debug.LogError("StageDataSet load failed. StageConstModel is missing.");
            }
        }

        public static StageModel GetModel(int id) => _models.GetValueOrDefault(id);
        public static float GetStageTime() => _constants != null ? _constants.StageTime : 0f;
        public static int GetMonsterCount() => _constants != null ? _constants.MonsterCount : 0;

        private static void AddAsset(ScriptableObject asset) {
            switch (asset) {
                case StageModel model:
                    if (!_models.TryAdd(model.Id, model)) {
                        Debug.LogError($"Duplicate StageModel ID: {model.Id} ({model.name})");
                    }
                    break;
                case StageConstModel constants:
                    if (_constants != null) {
                        Debug.LogError($"Duplicate StageConstModel: {constants.name}");
                    } else {
                        _constants = constants;
                    }
                    break;
            }
        }
    }
}
