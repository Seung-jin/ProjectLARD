using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Data;
using LSJ.Util;
using UnityEngine;

namespace LARD.Monster {
    public sealed class MonsterDataSet : IDataSet {
        public const string AddressableLabel = "Monster";

        private static readonly Dictionary<int, MonsterModel> _models = new();

        public async UniTask Load() {
            _models.Clear();

            IList<MonsterModel> assets = await AddressableManager.LoadAssetAsyncList<MonsterModel>(
                new[] { AddressableLabel }, AddAsset);

            if (assets == null) {
                Debug.LogError($"MonsterDataSet load failed. Addressable label: {AddressableLabel}");
            }
        }

        public static MonsterModel GetModel(int id) => _models.GetValueOrDefault(id);

        private static void AddAsset(MonsterModel model) {
            if (!_models.TryAdd(model.Id, model)) {
                Debug.LogError($"Duplicate MonsterModel ID: {model.Id} ({model.name})");
            }
        }
    }
}
