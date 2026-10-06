using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Data;
using LSJ.Util;
using UnityEngine;

namespace LARD.Upgrade {
    public sealed class UpgradeDataSet : IDataSet {
        public const string AddressableLabel = "Upgrade";

        private static readonly Dictionary<ArticleRace, Dictionary<int, UpgradeModel>> _models = new();
        private static readonly Dictionary<ArticleRace, PlayerUpgrade> _playerUpgrades = new();

        public async UniTask Load() {
            _models.Clear();
            foreach (ArticleRace race in Enum.GetValues(typeof(ArticleRace))) {
                _playerUpgrades.TryAdd(race, new PlayerUpgrade(race));
            }

            IList<UpgradeModel> assets = await AddressableManager.LoadAssetAsyncList<UpgradeModel>(
                new[] { AddressableLabel }, AddAsset);

            if (assets == null) {
                Debug.LogError($"UpgradeDataSet load failed. Addressable label: {AddressableLabel}");
            }
        }

        public static UpgradeModel GetModel(ArticleRace race, int level) {
            return _models.TryGetValue(race, out Dictionary<int, UpgradeModel> levels)
                ? levels.GetValueOrDefault(level)
                : null;
        }

        public static PlayerUpgrade GetPlayerUpgrade(ArticleRace race) => _playerUpgrades.GetValueOrDefault(race);

        private static void AddAsset(UpgradeModel model) {
            if (!_models.TryGetValue(model.Race, out Dictionary<int, UpgradeModel> levels)) {
                levels = new Dictionary<int, UpgradeModel>();
                _models.Add(model.Race, levels);
            }

            if (!levels.TryAdd(model.Level, model)) {
                Debug.LogError($"Duplicate UpgradeModel: {model.Race}, level {model.Level} ({model.name})");
            }
        }
    }
}
