using LARD.Article;
using LARD.Player;
using LARD.Stat;

namespace LARD.Upgrade {
    public static class UpgradeLogic {
        public static bool TryUpgrade(ArticleRace race) {
            PlayerUpgrade playerUpgrade = UpgradeDataSet.GetPlayerUpgrade(race);
            PlayerResource resource = PlayerDataSet.Resource;
            if (playerUpgrade == null || resource == null || playerUpgrade.Level == int.MaxValue) {
                return false;
            }

            UpgradeModel currentModel = UpgradeDataSet.GetModel(race, playerUpgrade.Level);
            UpgradeModel nextModel = UpgradeDataSet.GetModel(race, playerUpgrade.Level + 1);
            if (currentModel == null || nextModel == null || currentModel.NextLevelCost < 0) {
                return false;
            }

            if (!resource.TrySpend(ResourceType.Gold, currentModel.NextLevelCost)) {
                return false;
            }

            playerUpgrade.SetLevel(nextModel.Level);
            return true;
        }
    }
}
