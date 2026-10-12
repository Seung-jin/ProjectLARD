using System;
using System.Collections.Generic;
using LARD.Player;
using LARD.Stat;
using UnityEngine;

namespace LARD.Article {
    public sealed class ArticleLogic {
        private readonly IList<ArticleObj> _selectedArticles;
        private readonly ArticleFactory _factory;

        public ArticleLogic(IList<ArticleObj> selectedArticles, ArticleFactory factory) {
            _selectedArticles = selectedArticles ?? throw new ArgumentNullException(nameof(selectedArticles));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public bool TryUpgradeSelectedArticle() {
            if (_selectedArticles.Count != 1) {
                Debug.LogWarning($"[ArticlePromotion] Logic failed: exactly one article must be selected (selected: {_selectedArticles.Count}).");
                return false;
            }

            ArticleObj article = _selectedArticles[0];
            if (!TryGetUpgradeCost(article, out int cost, out string reason)) {
                Debug.LogWarning($"[ArticlePromotion] Logic failed: {reason}", article);
                return false;
            }

            ArticleTierModel currentTier = ArticleDataSet.GetTierModel(article.Tier);
            ArticleTierModel nextTier = ArticleDataSet.GetTierModel(article.Tier + 1);
            if (!ArticleDataSet.TryPickArticle(article.Model.Race, nextTier.Tier, out ArticleModel nextModel)) {
                Debug.LogWarning($"[ArticlePromotion] Logic failed: no article is available for race {article.Model.Race}, tier {nextTier.Tier}.", article);
                return false;
            }

            PlayerResource resource = PlayerDataSet.Resource;
            if (resource == null) {
                Debug.LogWarning("[ArticlePromotion] Logic failed: player resource is not loaded.", article);
                return false;
            }

            if (!resource.TrySpend(ResourceType.Silver, cost)) {
                Debug.LogWarning($"[ArticlePromotion] Logic failed: Silver spend was rejected (current: {resource.Silver}, cost: {cost}).", article);
                return false;
            }

            float roll = UnityEngine.Random.value;
            if (roll >= currentTier.UpgradeProbability) {
                Debug.Log($"[ArticlePromotion] Failed: article {article.Model.Id}, tier {article.Tier} -> {nextTier.Tier} (roll: {roll:P1}, chance: {currentTier.UpgradeProbability:P1}, cost: {cost} Silver).", article);
                return false;
            }

            ArticleModel previousModel = article.Model;
            int previousTier = article.Tier;
            Vector3 position = article.transform.position;
            Quaternion rotation = article.transform.rotation;

            ArticleObj promoted;
            try {
                promoted = _factory.Create(nextModel, position, rotation);
            } catch (Exception exception) {
                resource.Add(ResourceType.Silver, cost);
                Debug.LogError($"[ArticlePromotion] Logic failed: article {nextModel.Id} could not be created at tier {nextTier.Tier}; {cost} Silver refunded.");
                Debug.LogException(exception);
                return false;
            }

            _factory.Release(article);
            _selectedArticles[0] = promoted;
            Debug.Log($"[ArticlePromotion] Succeeded: article {previousModel.Id} -> {nextModel.Id}, race {nextModel.Race}, tier {previousTier} -> {nextTier.Tier} (cost: {cost} Silver).", promoted);
            return true;
        }

        public static bool TryGetUpgradeCost(ArticleObj article, out int cost) {
            return TryGetUpgradeCost(article, out cost, out _);
        }

        private static bool TryGetUpgradeCost(ArticleObj article, out int cost, out string reason) {
            cost = 0;
            reason = null;
            if (article == null || !article.gameObject.activeInHierarchy || article.IsInPool || article.Model == null) {
                reason = "the selected article is missing or inactive.";
                return false;
            }

            ArticleTierModel currentTier = ArticleDataSet.GetTierModel(article.Tier);
            if (currentTier == null) {
                reason = $"tier {article.Tier} data is missing.";
                return false;
            }

            if (currentTier.UpgradeCost <= 0 || currentTier.UpgradeProbability <= 0f) {
                reason = $"tier {article.Tier} has no valid promotion cost or chance.";
                return false;
            }

            ArticleTierModel nextTier = ArticleDataSet.GetTierModel(article.Tier + 1);
            if (nextTier == null || nextTier.Tier != article.Tier + 1) {
                reason = $"tier {article.Tier + 1} data is missing or invalid.";
                return false;
            }

            if (!ArticleDataSet.HasArticle(article.Model.Race, nextTier.Tier)) {
                reason = $"no article is available for race {article.Model.Race}, tier {nextTier.Tier}.";
                return false;
            }

            if (article.Model.Prefab == null) {
                reason = $"article {article.Model.Id} has no prefab.";
                return false;
            }

            cost = currentTier.UpgradeCost;
            return true;
        }
    }
}
