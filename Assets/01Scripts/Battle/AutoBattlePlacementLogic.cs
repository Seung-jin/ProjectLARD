using System.Collections.Generic;
using LARD.Article;
using UnityEngine;

namespace LARD.Battle {
    /// <summary>자동 전투 테스트에서만 사용하는 기물 배치.</summary>
    internal sealed class AutoBattlePlacementLogic {
        private const float PlacementGap = 0.05f;
        private const int UpperLeft = 0;
        private const int LowerLeft = 1;
        private const int LowerRight = 2;

        private readonly Dictionary<int, int> _placedSectors = new();

        public void Transfer(int previousId, int promotedId) {
            if (!_placedSectors.TryGetValue(previousId, out int sector)) return;
            _placedSectors.Remove(previousId);
            _placedSectors[promotedId] = sector;
        }

        public List<ArticleRace> GetRacesByCount() {
            Dictionary<ArticleRace, int> counts = new();
            foreach (ArticleRace race in System.Enum.GetValues(typeof(ArticleRace))) counts[race] = 0;
            foreach (ArticleObj article in Object.FindObjectsByType<ArticleObj>(FindObjectsSortMode.None)) {
                if (article == null || !article.gameObject.activeInHierarchy || article.IsInPool || article.Model == null)
                    continue;
                counts[article.Model.Race]++;
            }
            List<ArticleRace> races = new(counts.Keys);
            races.Sort((left, right) => {
                int countOrder = counts[right].CompareTo(counts[left]);
                return countOrder != 0 ? countOrder : left.CompareTo(right);
            });
            return races;
        }

        public void Place(Map map, ArticleObj newlySummoned, bool groupByRace) {
            if (map == null) {
                Debug.LogWarning("[AutoBattleTest] Cannot place articles: Map is missing.");
                return;
            }

            List<ArticleObj> articles = new();
            float maxRadius = 0f;
            foreach (ArticleObj article in Object.FindObjectsByType<ArticleObj>(FindObjectsSortMode.None)) {
                if (article == null || !article.gameObject.activeInHierarchy || article.IsInPool || article.Model == null)
                    continue;
                articles.Add(article);
                maxRadius = Mathf.Max(maxRadius, GetRadius(article));
            }

            Dictionary<ArticleRace, int> sectorByRace = new();
            if (groupByRace) {
                List<ArticleRace> races = GetRacesByCount();
                for (int i = 0; i < races.Count; i++)
                    sectorByRace[races[i]] = Mathf.Min(i, LowerRight);
            }

            List<Vector2>[] sectorSlots = { new(), new(), new() };
            List<Vector2> edgeSlots = new();
            List<Vector2> allSlots = new();
            float limit = Mathf.Max(0f, map.ArticleAreaSize * 0.5f - maxRadius);
            float spacing = maxRadius * 2f + PlacementGap;
            int sideCount = Mathf.Max(1, Mathf.FloorToInt(limit * 2f / spacing) + 1);
            float step = sideCount > 1 ? limit * 2f / (sideCount - 1) : 0f;
            Vector2 center = map.ArticleSpawnPosition;
            int middle = (sideCount - 1) / 2;
            for (int y = 0; y < sideCount; y++) {
                for (int x = 0; x < sideCount; x++) {
                    Vector2 spot = center + new Vector2(-limit + x * step, -limit + y * step);
                    allSlots.Add(spot);
                    if (x == 0 || x == sideCount - 1 || y == 0 || y == sideCount - 1)
                        edgeSlots.Add(spot);
                    if (x <= middle && y >= middle) sectorSlots[UpperLeft].Add(spot);
                    else if (x <= middle) sectorSlots[LowerLeft].Add(spot);
                    else if (y < middle) sectorSlots[LowerRight].Add(spot);
                }
            }

            Vector2[] anchors = {
                center + new Vector2(-limit, limit),
                center + new Vector2(-limit, -limit),
                center + new Vector2(limit, -limit),
            };
            for (int sector = 0; sector < sectorSlots.Length; sector++) {
                Vector2 anchor = anchors[sector];
                sectorSlots[sector].Sort((left, right) =>
                    (left - anchor).sqrMagnitude.CompareTo((right - anchor).sqrMagnitude));
            }

            Dictionary<int, int> nextPlacements = new();
            List<Reservation> occupied = new();
            List<ArticleObj> toPlace = new();
            foreach (ArticleObj article in articles) {
                int sector = groupByRace ? sectorByRace[article.Model.Race] : UpperLeft;
                if (newlySummoned != null && article != newlySummoned &&
                    _placedSectors.TryGetValue(article.Id, out int previousSector) &&
                    previousSector == sector) {
                    nextPlacements[article.Id] = sector;
                    occupied.Add(new Reservation(article.transform.position, GetRadius(article)));
                } else {
                    toPlace.Add(article);
                }
            }

            toPlace.Sort((left, right) => {
                int leftSector = groupByRace ? sectorByRace[left.Model.Race] : UpperLeft;
                int rightSector = groupByRace ? sectorByRace[right.Model.Race] : UpperLeft;
                int sectorOrder = leftSector.CompareTo(rightSector);
                return sectorOrder != 0 ? sectorOrder : left.Id.CompareTo(right.Id);
            });
            bool moved = false;
            foreach (ArticleObj article in toPlace) {
                int sector = groupByRace ? sectorByRace[article.Model.Race] : UpperLeft;
                float radius = GetRadius(article);
                Vector2 anchor = anchors[sector];
                if (!TryFindFreeSlot(sectorSlots[sector], radius, occupied, out Vector2 position)) {
                    edgeSlots.Sort((left, right) =>
                        (left - anchor).sqrMagnitude.CompareTo((right - anchor).sqrMagnitude));
                    if (!TryFindFreeSlot(edgeSlots, radius, occupied, out position)) {
                        allSlots.Sort((left, right) =>
                            (left - anchor).sqrMagnitude.CompareTo((right - anchor).sqrMagnitude));
                        if (!TryFindFreeSlot(allSlots, radius, occupied, out position)) {
                            Debug.LogWarning($"[AutoBattleTest] No free placement for article {article.Id} ({article.Model.Race}).");
                            occupied.Add(new Reservation(article.transform.position, radius));
                            continue;
                        }
                    }
                }

                if (article.MoveComponent is MoveComponentDestination movement) movement.Stop();
                Rigidbody2D body = article.GetComponent<Rigidbody2D>();
                article.transform.position = new Vector3(position.x, position.y, article.transform.position.z);
                if (body != null) body.position = position;
                occupied.Add(new Reservation(position, radius));
                nextPlacements[article.Id] = sector;
                moved = true;
            }

            _placedSectors.Clear();
            foreach (KeyValuePair<int, int> placement in nextPlacements)
                _placedSectors.Add(placement.Key, placement.Value);
            if (moved) Physics2D.SyncTransforms();
        }

        private static float GetRadius(ArticleObj article) {
            CapsuleCollider2D collider = article.GetComponent<CapsuleCollider2D>();
            if (collider == null) return 0.2f;
            Vector3 scale = collider.transform.lossyScale;
            float x = (Mathf.Abs(collider.size.x) * 0.5f + Mathf.Abs(collider.offset.x)) * Mathf.Abs(scale.x);
            float y = (Mathf.Abs(collider.size.y) * 0.5f + Mathf.Abs(collider.offset.y)) * Mathf.Abs(scale.y);
            return Mathf.Max(0.2f, x, y);
        }

        private static bool TryFindFreeSlot(List<Vector2> slots, float radius,
            List<Reservation> occupied, out Vector2 position) {
            foreach (Vector2 slot in slots) {
                bool free = true;
                foreach (Reservation reservation in occupied) {
                    float distance = radius + reservation.Radius + PlacementGap;
                    if ((slot - reservation.Position).sqrMagnitude < distance * distance) {
                        free = false;
                        break;
                    }
                }
                if (!free) continue;
                position = slot;
                return true;
            }
            position = default;
            return false;
        }

        private readonly struct Reservation {
            public readonly Vector2 Position;
            public readonly float Radius;

            public Reservation(Vector2 position, float radius) {
                Position = position;
                Radius = radius;
            }
        }
    }
}
