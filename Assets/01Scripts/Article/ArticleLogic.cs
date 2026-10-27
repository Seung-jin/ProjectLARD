using System;
using System.Collections.Generic;
using LARD.Player;
using LARD.Stat;
using UnityEngine;

namespace LARD.Article {
    /// <summary>선택한 기물의 승급과 판매를 담당한다.</summary>
    public sealed class ArticleLogic {
        private const int PromotionMaterialCount = 3;

        private readonly List<ArticleObj> _selectedArticles;
        private readonly ArticleFactory _factory;

        public ArticleLogic(List<ArticleObj> selectedArticles, ArticleFactory factory) {
            _selectedArticles = selectedArticles ?? throw new ArgumentNullException(nameof(selectedArticles));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>선택한 같은 티어 기물 3마리를 다음 티어 기물 1마리로 합친다.</summary>
        public bool TryUpgradeSelectedArticles() => TryPromoteMaterials(_selectedArticles);

        public bool CanUpgradeAvailableArticles(int targetTier, ArticleRace? race) =>
            TryFindMaterials(targetTier, race, out _, out _);

        public bool TryUpgradeAvailableArticles(int targetTier, ArticleRace? race) {
            if (!TryFindMaterials(targetTier, race, out List<ArticleObj> materials, out string reason)) {
                Debug.LogWarning($"[ArticlePromotion] Logic failed: {reason}");
                return false;
            }

            return TryPromoteMaterials(materials);
        }

        private bool TryPromoteMaterials(IReadOnlyList<ArticleObj> materials) {
            if (!TryGetPromotionOptions(materials, out ArticleRace[] races, out int nextTier, out string reason)) {
                Debug.LogWarning($"[ArticlePromotion] Logic failed: {reason}");
                return false;
            }

            ArticleRace race = races[UnityEngine.Random.Range(0, races.Length)];
            if (!ArticleDataSet.TryPickArticle(race, nextTier, out ArticleModel nextModel) || nextModel.Prefab == null) {
                Debug.LogWarning($"[ArticlePromotion] Logic failed: no summonable article for race {race}, tier {nextTier}.");
                return false;
            }

            ArticleObj[] consumed = { materials[0], materials[1], materials[2] };
            Vector3 position = consumed[0].transform.position;
            Quaternion rotation = consumed[0].transform.rotation;
            ArticleObj promoted;
            try {
                promoted = _factory.Create(nextModel, position, rotation);
            } catch (Exception exception) {
                Debug.LogError($"[ArticlePromotion] Logic failed: article {nextModel.Id} could not be created. Materials were not consumed.");
                Debug.LogException(exception);
                return false;
            }

            foreach (ArticleObj material in consumed) {
                _factory.Release(material);
            }

            _selectedArticles.Clear();
            _selectedArticles.Add(promoted);
            Debug.Log($"[ArticlePromotion] Succeeded: 3 articles -> article {nextModel.Id}, race {race}, tier {nextTier}.", promoted);
            return true;
        }

        private static bool TryFindMaterials(int targetTier, ArticleRace? race, out List<ArticleObj> materials,
                                             out string reason) {
            materials = new List<ArticleObj>(PromotionMaterialCount);
            reason = null;
            if (targetTier < 2 || ArticleDataSet.GetTierModel(targetTier) == null) {
                reason = $"tier {targetTier} data is missing or invalid.";
                return false;
            }

            ArticleObj[] available = UnityEngine.Object.FindObjectsByType<ArticleObj>(FindObjectsSortMode.None);
            Array.Sort(available, (left, right) => left.Id.CompareTo(right.Id));
            ArticleRace[] requiredRaces = race.HasValue
                ? new[] { race.Value, race.Value, race.Value }
                : new[] { ArticleRace.Beastkin, ArticleRace.Mage, ArticleRace.Warrior };
            foreach (ArticleRace requiredRace in requiredRaces) {
                ArticleObj found = null;
                foreach (ArticleObj article in available) {
                    if (article == null || !article.gameObject.activeInHierarchy || article.IsInPool ||
                        article.Model == null || article.Tier != targetTier - 1 ||
                        article.Model.Race != requiredRace || materials.Contains(article)) continue;
                    found = article;
                    break;
                }

                if (found == null) {
                    reason = $"not enough tier {targetTier - 1} articles for race {requiredRace}.";
                    return false;
                }
                materials.Add(found);
            }

            return CanPromoteArticles(materials, out reason);
        }

        public bool CanUpgradeSelectedArticles(out string reason) =>
            TryGetPromotionOptions(_selectedArticles, out _, out _, out reason);

        public static bool CanPromoteArticles(IReadOnlyList<ArticleObj> articles, out string reason) =>
            TryGetPromotionOptions(articles, out _, out _, out reason);

        private static bool TryGetPromotionOptions(IReadOnlyList<ArticleObj> articles, out ArticleRace[] races,
                                                    out int nextTier, out string reason) {
            races = null;
            nextTier = 0;
            reason = null;
            if (articles == null || articles.Count != PromotionMaterialCount) {
                reason = $"exactly {PromotionMaterialCount} articles must be selected (selected: {articles?.Count ?? 0}).";
                return false;
            }

            var uniqueArticles = new HashSet<ArticleObj>();
            var selectedRaces = new HashSet<ArticleRace>();
            int tier = 0;
            foreach (ArticleObj article in articles) {
                if (article == null || !article.gameObject.activeInHierarchy || article.IsInPool || article.Model == null ||
                    !uniqueArticles.Add(article)) {
                    reason = "a selected article is missing, inactive, or duplicated.";
                    return false;
                }

                if (tier == 0) tier = article.Tier;
                if (article.Tier != tier) {
                    reason = "all three articles must have the same tier.";
                    return false;
                }
                selectedRaces.Add(article.Model.Race);
            }

            nextTier = tier + 1;
            if (ArticleDataSet.GetTierModel(nextTier) == null) {
                reason = $"tier {nextTier} data is missing.";
                return false;
            }

            if (selectedRaces.Count == 1) {
                ArticleRace race = articles[0].Model.Race;
                if (!ArticleDataSet.HasArticle(race, nextTier)) {
                    reason = $"no article is available for race {race}, tier {nextTier}.";
                    return false;
                }
                races = new[] { race };
                return true;
            }

            if (selectedRaces.Count != 3 || !selectedRaces.Contains(ArticleRace.Beastkin) ||
                !selectedRaces.Contains(ArticleRace.Mage) || !selectedRaces.Contains(ArticleRace.Warrior)) {
                reason = "select three of the same race or one from each race.";
                return false;
            }

            foreach (ArticleRace selectedRace in selectedRaces) {
                if (ArticleDataSet.HasArticle(selectedRace, nextTier)) continue;
                reason = $"no article is available for race {selectedRace}, tier {nextTier}.";
                return false;
            }

            races = new[] { ArticleRace.Beastkin, ArticleRace.Mage, ArticleRace.Warrior };
            return true;
        }

        /// <summary>선택한 기물을 반환하고 판매 대금을 지급한다.</summary>
        public bool TrySellSelectedArticle() {
            if (_selectedArticles.Count != 1) {
                Debug.LogWarning($"[ArticleSale] Exactly one article must be selected (selected: {_selectedArticles.Count}).");
                return false;
            }

            ArticleObj article = _selectedArticles[0];
            if (!TryGetSaleCost(article, out int saleCost)) {
                Debug.LogWarning("[ArticleSale] Selected article or tier sale data is invalid.", article);
                return false;
            }

            PlayerResource resource = PlayerDataSet.Resource;
            if (resource == null) {
                Debug.LogWarning("[ArticleSale] Player resource is not loaded.", article);
                return false;
            }

            int modelId = article.Model.Id;
            int tier = article.Tier;
            _factory.Release(article);
            _selectedArticles.Clear();
            resource.Add(ResourceType.Silver, saleCost);
            Debug.Log($"[ArticleSale] Sold article {modelId}, tier {tier} for {saleCost} Silver.");
            return true;
        }

        public static bool TryGetSaleCost(ArticleObj article, out int cost) {
            cost = 0;
            if (article == null || !article.gameObject.activeInHierarchy || article.IsInPool || article.Model == null) {
                return false;
            }

            ArticleTierModel tierModel = ArticleDataSet.GetTierModel(article.Tier);
            if (tierModel == null || tierModel.SaleCost <= 0) {
                return false;
            }

            cost = tierModel.SaleCost;
            return true;
        }
    }
}
