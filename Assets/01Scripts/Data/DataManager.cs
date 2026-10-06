using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Monster;
using LARD.Player;
using LARD.Shop;
using LARD.Stage;
using LARD.Upgrade;

namespace LARD.Data {
    public static class DataManager {
        private static readonly List<IDataSet> _dataSets = new() {
            new ArticleDataSet(),
            new MonsterDataSet(),
            new StageDataSet(),
            new ShopDataSet(),
            new UpgradeDataSet(),
            new PlayerDataSet(),
        };

        public static async UniTask Init() {
            UniTask[] loadTasks = new UniTask[_dataSets.Count];
            for (var i = 0; i < loadTasks.Length; i++) {
                loadTasks[i] = _dataSets[i].Load();
            }

            await UniTask.WhenAll(loadTasks);
        }
    }


}
