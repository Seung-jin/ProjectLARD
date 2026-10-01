using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Player;
using LARD.Shop;

namespace LARD.Data {
    public static class DataManager {
        private static readonly List<IDataSet> _dataSets = new() {
            new ArticleDataSet(),
            new ShopDataSet(),
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
