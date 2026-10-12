using LARD.Article;
using LARD.Player;
using LARD.Stage;
using LARD.Stat;
using UnityEngine;

namespace LARD.Shop {
    public static class ShopLogic {
        private const int ShopConstId = 1;
        private const int ShopProbId = 1;

        public static bool TryPickAndSummonArticle() {
            PlayerResource resource = PlayerDataSet.Resource;
            ShopConstModel constants = ShopDataSet.GetConstModel(ShopConstId);
            ShopProbModel probabilities = ShopDataSet.GetProbModel(ShopProbId);
            if (resource == null || constants == null || probabilities == null) {
                Debug.LogWarning("[ShopLogic] Article summon failed: player or shop data is not loaded.");
                return false;
            }

            int cost = constants.PurchaseCost;
            if (resource.Silver < cost) {
                Debug.LogWarning($"[ShopLogic] Article summon failed: insufficient Silver (current: {resource.Silver}, cost: {cost}).");
                return false;
            }

            if (!probabilities.TryPickTier(out int tierNumber) ||
                !ArticleDataSet.TryPickArticle(tierNumber, out ArticleModel model)) {
                Debug.LogWarning("[ShopLogic] Article summon failed: the selected tier has no drawable article or the tier probabilities are empty.");
                return false;
            }

            if (!resource.TrySpend(ResourceType.Silver, cost)) {
                Debug.LogWarning($"[ShopLogic] Article summon failed: Silver spend was rejected (current: {resource.Silver}, cost: {cost}).");
                return false;
            }

            if (!StageLogic.TrySummonArticle(model.Id, out ArticleObj article)) {
                resource.Add(ResourceType.Silver, cost);
                return false;
            }

            Debug.Log($"[Battle] Summoned article {article.Model.Id}, tier {article.Tier}.", article);
            return true;
        }
    }
}
