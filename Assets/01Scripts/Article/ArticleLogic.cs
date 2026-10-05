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
                return false;
            }

            ArticleObj article = _selectedArticles[0];
            if (article == null || !article.gameObject.activeInHierarchy || article.IsInPool || article.Model == null) {
                return false;
            }

            ArticleTierModel currentTier = ArticleDataSet.GetTierModel(article.Model.Id, article.Tier);
            if (currentTier == null || currentTier.Article != article.Model ||
                currentTier.UpgradeCost <= 0 || currentTier.UpgradeProbability <= 0f) {
                return false;
            }

            ArticleTierModel nextTier = currentTier.NextTier;
            if (nextTier == null || nextTier.Tier != article.Tier + 1 ||
                nextTier.Article == null || nextTier.Article.Prefab == null ||
                ArticleDataSet.GetTierModel(nextTier.Article.Id, nextTier.Tier) != nextTier) {
                return false;
            }

            PlayerResource resource = PlayerDataSet.Resource;
            if (resource == null || !resource.TrySpend(ResourceType.Silver, currentTier.UpgradeCost)) {
                return false;
            }

            if (UnityEngine.Random.value >= currentTier.UpgradeProbability) {
                return false;
            }

            ArticleObj upgraded;
            try {
                upgraded = _factory.Create(nextTier, article.transform.position, article.transform.rotation);
            } catch (Exception exception) {
                resource.Add(ResourceType.Silver, currentTier.UpgradeCost);
                Debug.LogException(exception);
                return false;
            }

            _selectedArticles[0] = upgraded;
            _factory.Release(article);
            return true;
        }
    }
}
