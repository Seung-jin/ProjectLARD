using System;
using System.Collections.Generic;
using LARD.Article;
using LARD.Battle;
using UnityEngine;

namespace LARD.Stage {
    /// <summary>기물 선택, 소환 위치 확보와 편대 이동을 관리한다.</summary>
    internal static class StageArticleLogic {
        private const float PlacementGap = 0.05f;

        private static int _articleSelectionLayerMask;
        private static Map _map;
        private static float _formationGap;
        private static List<ArticleObj> _selectedArticles;
        private static ArticleFactory _articleFactory;
        private static bool _init;

        public static ArticleFactory ArticleFactory => _articleFactory;

        public static void Init(Map map, float formationGap, List<ArticleObj> selectedArticles) {
            if (_init) {
                Dispose();
            }

            _map = map;
            _articleSelectionLayerMask = LayerMask.GetMask("ArticleSelection");
            _formationGap = formationGap;
            _selectedArticles = selectedArticles ?? throw new ArgumentNullException(nameof(selectedArticles));
            _articleFactory = new ArticleFactory();
            _init = true;
        }

        public static void Select(ArticleObj article) {
            if (_selectedArticles != null && article != null && !_selectedArticles.Contains(article)) {
                _selectedArticles.Add(article);
            }
        }

        public static void Deselect(ArticleObj article) {
            _selectedArticles?.Remove(article);
        }

        public static void ClearSelection() {
            _selectedArticles?.Clear();
        }

        public static void SelectAt(Vector2 position) {
            if (!_init) {
                return;
            }

            Collider2D[] hits = Physics2D.OverlapPointAll(position, _articleSelectionLayerMask);
            Debug.Log($"[BattleInput] Point query at {position} found {hits.Length} colliders.");
            ClearSelection();

            foreach (Collider2D hit in hits) {
                ArticleObj article = hit.GetComponentInParent<ArticleObj>();
                if (article != null && article.gameObject.activeInHierarchy) {
                    Select(article);
                    Debug.Log($"[BattleInput] Selected {article.name} (ID {article.Id}).", article);
                    return;
                }
            }

            Debug.Log("[BattleInput] No article selected; selection cleared.");
        }

        public static void SelectArea(Vector2 start, Vector2 end) {
            if (!_init) {
                return;
            }

            Vector2 min = Vector2.Min(start, end);
            Vector2 max = Vector2.Max(start, end);
            Collider2D[] hits = Physics2D.OverlapAreaAll(min, max, _articleSelectionLayerMask);
            Debug.Log($"[BattleInput] Area query from {min} to {max} found {hits.Length} colliders.");

            ClearSelection();
            foreach (Collider2D hit in hits) {
                ArticleObj article = hit.GetComponentInParent<ArticleObj>();
                if (article != null && article.gameObject.activeInHierarchy) {
                    Select(article);
                }
            }

            Debug.Log($"[BattleInput] Drag selected {_selectedArticles.Count} articles.");
        }

        /// <summary>소환 위치를 확보하고 기존 기물의 배치를 조정한 뒤 새 기물을 생성한다.</summary>
        public static bool TrySummonArticle(int articleId, out ArticleObj article) {
            article = null;
            if (!_init || _map == null || _articleFactory == null) {
                Debug.LogWarning("[StageLogic] Article summon failed: stage is not initialized or Map is missing.");
                return false;
            }

            ArticleModel model = ArticleDataSet.GetModel(articleId);
            if (model == null || model.Prefab == null) {
                Debug.LogWarning($"[StageLogic] Article summon failed: article ID {articleId} is not loaded or has no prefab.");
                return false;
            }

            if (!TryPlanDisplacements(_map, model.Prefab, out List<Displacement> displacements)) {
                Debug.LogWarning($"[StageLogic] Article summon failed: no free position for article {articleId} near {_map.ArticleSpawnPosition} (area size: {_map.ArticleAreaSize}).");
                return false;
            }

            try {
                article = _articleFactory.Create(model, _map.ArticleSpawnPosition, Quaternion.identity);
            } catch (Exception exception) {
                Debug.LogError($"[StageLogic] Article summon failed while creating article {articleId}.");
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

        /// <summary>선택한 기물을 목적지 주변의 편대 위치로 이동시킨다.</summary>
        public static void MoveSelectedArticles(Vector2 destination) {
            if (!_init) {
                return;
            }

            _selectedArticles.RemoveAll(article => article == null || !article.gameObject.activeInHierarchy || article.IsInPool);

            int count = _selectedArticles.Count;
            if (count == 0) {
                return;
            }

            float width = 0f;
            float height = 0f;
            foreach (ArticleObj article in _selectedArticles) {
                CapsuleCollider2D collider = article.GetComponent<CapsuleCollider2D>();
                if (collider != null) {
                    width = Mathf.Max(width, collider.bounds.size.x);
                    height = Mathf.Max(height, collider.bounds.size.y);
                }
            }

            float stepX = width + _formationGap;
            float stepY = height + _formationGap;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt((float)count / columns);

            if (_map != null) {
                Vector2 formationHalfExtents = new Vector2((columns - 1) * stepX * 0.5f + width * 0.5f,
                                                           (rows - 1) * stepY * 0.5f + height * 0.5f);
                destination = _map.ClampArticlePosition(destination, formationHalfExtents);
            }

            var reservations = new List<MoveComponentDestination.DestinationReservation>(count);
            foreach (ArticleObj article in _selectedArticles) {
                reservations.Add(new MoveComponentDestination.DestinationReservation(
                    article.transform.position, GetRadius(article)));
            }

            for (int i = 0; i < count; i++) {
                ArticleObj article = _selectedArticles[i];
                reservations.RemoveAt(0);
                if (article.MoveComponent is not MoveComponentDestination movement) {
                    reservations.Add(new MoveComponentDestination.DestinationReservation(
                        article.transform.position, GetRadius(article)));
                    continue;
                }

                int row = i / columns;
                int column = i % columns;
                Vector2 offset = new Vector2((column - (columns - 1) * 0.5f) * stepX,
                                             ((rows - 1) * 0.5f - row) * stepY);
                if (movement.TrySetDestination(destination + offset, reservations, _selectedArticles,
                                               out Vector2 assigned)) {
                    reservations.Add(new MoveComponentDestination.DestinationReservation(assigned, GetRadius(article)));
                } else {
                    reservations.Add(new MoveComponentDestination.DestinationReservation(
                        article.transform.position, GetRadius(article)));
                }
            }
        }

        public static void Dispose() {
            if (!_init) {
                return;
            }

            _init = false;
            _selectedArticles = null;
            _articleFactory = null;
            _map = null;
        }
    }
}
