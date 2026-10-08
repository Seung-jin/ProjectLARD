using System;
using System.Collections.Generic;
using LARD.Article;
using LARD.Battle;
using LARD.Player;
using LARD.Stat;
using UnityEngine;

namespace LARD.Shop {
    public static class ShopLogic {
        private const int ShopConstId = 1;
        private const int ShopProbId = 1;
        private const float PlacementGap = 0.05f;

        public static bool TryDrawArticle(Map map, ArticleFactory factory, out ArticleObj article) {
            article = null;
            if (map == null) {
                Debug.LogError("[ShopLogic] Article draw failed: Map is missing.");
                return false;
            }

            if (factory == null) {
                Debug.LogError("[ShopLogic] Article draw failed: ArticleFactory is missing.");
                return false;
            }

            PlayerResource resource = PlayerDataSet.Resource;
            ShopConstModel constants = ShopDataSet.GetConstModel(ShopConstId);
            ShopProbModel probabilities = ShopDataSet.GetProbModel(ShopProbId);
            if (resource == null) {
                Debug.LogWarning("[ShopLogic] Article draw failed: PlayerResource is not loaded.");
                return false;
            }

            if (constants == null) {
                Debug.LogWarning($"[ShopLogic] Article draw failed: ShopConstModel ID {ShopConstId} is not loaded.");
                return false;
            }

            if (probabilities == null) {
                Debug.LogWarning($"[ShopLogic] Article draw failed: ShopProbModel ID {ShopProbId} is not loaded.");
                return false;
            }

            int cost = constants.PurchaseCost;
            if (resource.Silver < cost) {
                Debug.LogWarning($"[ShopLogic] Article draw failed: insufficient Silver (current: {resource.Silver}, cost: {cost}).");
                return false;
            }

            if (!probabilities.TryPickTier(out int tierNumber) ||
                !ArticleDataSet.TryPickArticleTier(tierNumber, out ArticleTierModel tier)) {
                Debug.LogWarning("[ShopLogic] Article draw failed: the selected tier has no drawable article or the tier probabilities are empty.");
                return false;
            }

            if (!TryPlanDisplacements(map, tier.Article.Prefab, out List<Displacement> displacements)) {
                Debug.LogWarning($"[ShopLogic] Article draw failed: no free position for article {tier.Article.Id} near {map.ArticleSpawnPosition} (area size: {map.ArticleAreaSize}).");
                return false;
            }

            if (!resource.TrySpend(ResourceType.Silver, cost)) {
                Debug.LogWarning($"[ShopLogic] Article draw failed: Silver spend was rejected (current: {resource.Silver}, cost: {cost}).");
                return false;
            }

            try {
                article = factory.Create(tier, map.ArticleSpawnPosition, Quaternion.identity);
            } catch (Exception exception) {
                resource.Add(ResourceType.Silver, cost);
                Debug.LogError($"[ShopLogic] Article draw failed while creating article {tier.Article.Id}; refunded {cost} Silver.");
                Debug.LogException(exception);
                return false;
            }

            foreach (Displacement displacement in displacements) {
                if (displacement.Article.MoveComponent is MoveComponentDestination movement) {
                    movement.Stop();
                }

                Rigidbody2D body = displacement.Article.GetComponent<Rigidbody2D>();
                if (body != null) {
                    body.position = displacement.Position;
                } else {
                    displacement.Article.transform.position = displacement.Position;
                }
            }

            Physics2D.SyncTransforms();
            return true;
        }

        private static bool TryPlanDisplacements(Map map, ArticleObj prefab, out List<Displacement> displacements) {
            displacements = new List<Displacement>();
            Vector2 spawn = map.ArticleSpawnPosition;
            float spawnRadius = GetRadius(prefab);

            List<ArticleObj> articles = new();
            List<Vector2> positions = new();
            List<float> radii = new();
            float maxRadius = spawnRadius;
            foreach (ArticleObj existing in UnityEngine.Object.FindObjectsByType<ArticleObj>(FindObjectsSortMode.None)) {
                if (!existing.gameObject.activeInHierarchy || existing.IsInPool) {
                    continue;
                }

                float radius = GetRadius(existing);
                articles.Add(existing);
                positions.Add(existing.transform.position);
                radii.Add(radius);
                maxRadius = Mathf.Max(maxRadius, radius);
            }

            float step = maxRadius * 2f + PlacementGap;
            int maxRing = Mathf.CeilToInt(map.ArticleAreaSize / step) + 1;
            for (int i = 0; i < articles.Count; i++) {
                float requiredDistance = radii[i] + spawnRadius + PlacementGap;
                if ((positions[i] - spawn).sqrMagnitude >= requiredDistance * requiredDistance) {
                    continue;
                }

                if (!TryFindFreePosition(map, spawn, spawnRadius, step, maxRing, i, positions, radii, out Vector2 destination)) {
                    displacements.Clear();
                    return false;
                }

                positions[i] = destination;
                displacements.Add(new Displacement(articles[i], destination));
            }

            return true;
        }

        private static bool TryFindFreePosition(Map map, Vector2 spawn, float spawnRadius, float step,
            int maxRing, int movingIndex, List<Vector2> positions, List<float> radii, out Vector2 destination) {
            for (int ring = 1; ring <= maxRing; ring++) {
                for (int x = -ring; x <= ring; x++) {
                    for (int y = -ring; y <= ring; y++) {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != ring) {
                            continue;
                        }

                        Vector2 candidate = spawn + new Vector2(x * step, y * step);
                        float spawnDistance = radii[movingIndex] + spawnRadius + PlacementGap;
                        if ((candidate - map.ClampArticlePosition(candidate, Vector2.one * radii[movingIndex])).sqrMagnitude > 0.000001f ||
                            (candidate - spawn).sqrMagnitude < spawnDistance * spawnDistance) {
                            continue;
                        }

                        bool occupied = false;
                        for (int i = 0; i < positions.Count; i++) {
                            if (i == movingIndex) {
                                continue;
                            }

                            float minDistance = radii[movingIndex] + radii[i] + PlacementGap;
                            if ((candidate - positions[i]).sqrMagnitude < minDistance * minDistance) {
                                occupied = true;
                                break;
                            }
                        }

                        if (!occupied) {
                            destination = candidate;
                            return true;
                        }
                    }
                }
            }

            destination = default;
            return false;
        }

        private static float GetRadius(ArticleObj article) {
            CapsuleCollider2D collider = article.GetComponent<CapsuleCollider2D>();
            if (collider == null) {
                return 0.2f;
            }

            Vector3 scale = collider.transform.lossyScale;
            float x = (Mathf.Abs(collider.size.x) * 0.5f + Mathf.Abs(collider.offset.x)) * Mathf.Abs(scale.x);
            float y = (Mathf.Abs(collider.size.y) * 0.5f + Mathf.Abs(collider.offset.y)) * Mathf.Abs(scale.y);
            return Mathf.Max(0.2f, x, y);
        }

        private readonly struct Displacement {
            public readonly ArticleObj Article;
            public readonly Vector2 Position;

            public Displacement(ArticleObj article, Vector2 position) {
                Article = article;
                Position = position;
            }
        }
    }
}
