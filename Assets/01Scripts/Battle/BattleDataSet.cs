using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Data;
using LSJ.Util;
using UnityEngine;

namespace LARD.Battle {
    public sealed class BattleDataSet : IDataSet {
        public const string AddressableLabel = "Battle";

        public static BattleConstModel Constants { get; private set; }

        public async UniTask Load() {
            Constants = null;

            IList<ScriptableObject> assets = await AddressableManager.LoadAssetAsyncList<ScriptableObject>(
                new[] { AddressableLabel }, AddAsset);

            if (assets == null || Constants == null) {
                Debug.LogError($"BattleConstModel load failed. Addressable label: {AddressableLabel}");
            }
        }

        private static void AddAsset(ScriptableObject asset) {
            if (asset is not BattleConstModel model) {
                return;
            }

            if (Constants != null) {
                Debug.LogError($"Duplicate BattleConstModel: {model.name}");
                return;
            }

            Constants = model;
        }
    }
}
