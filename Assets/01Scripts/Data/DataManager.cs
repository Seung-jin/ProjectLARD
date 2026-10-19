using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Battle;
using LARD.Monster;
using LARD.Player;
using LARD.Shop;
using LARD.Stage;
using LARD.Upgrade;

namespace LARD.Data {
    /// <summary>
    /// 전투 시작에 필요한 데이터셋을 등록하고 비동기로 함께 로드한다.
    /// </summary>
    public static class DataManager {
        private static readonly List<IDataSet> _dataSets = new() {
            new ArticleDataSet(),
            new BattleDataSet(),
            new MonsterDataSet(),
            new StageDataSet(),
            new ShopDataSet(),
            new UpgradeDataSet(),
            new PlayerDataSet(),
        };

        /// <summary>등록된 모든 데이터셋의 로드가 끝날 때까지 기다린다.</summary>
        public static async UniTask Init() {
            UniTask[] loadTasks = new UniTask[_dataSets.Count];
            for (var i = 0; i < loadTasks.Length; i++) {
                loadTasks[i] = _dataSets[i].Load();
            }

            await UniTask.WhenAll(loadTasks);
        }
    }


}
