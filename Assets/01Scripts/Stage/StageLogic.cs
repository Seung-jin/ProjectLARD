using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Battle;
using LARD.Monster;
using UnityEngine;

namespace LARD.Stage {
    /// <summary>
    /// 기물 배치와 이동, 스테이지별 몬스터 생성, 제한 시간 및 승리 조건을 관리한다.
    /// </summary>
    public static class StageLogic {
        private static readonly TimeSpan MonsterSpawnInterval = TimeSpan.FromSeconds(0.5);
        private const int ClearedStageWaitSeconds = 20;
        private static readonly int ArticleSelectionLayerMask = LayerMask.GetMask("ArticleSelection");
        private const float PlacementGap = 0.05f;

        private static Map _map;
        private static float _formationGap;
        private static List<ArticleObj> _selectedArticles;
        private static Action<StageModel> _startStage;
        private static Action<int> _stageTimeChanged;
        private static Action _completeVictory;
        private static Func<bool, bool> _stageTimeExpired;
        private static Action _monsterDied;
        private static ArticleFactory _articleFactory;
        private static MonsterFactory _monsterFactory;
        private static readonly List<MonsterObj> _aliveMonsters = new();
        private static readonly HashSet<MonsterObj> _currentStageMonsters = new();

        private static CancellationTokenSource _stageSequenceCancellation;
        private static bool _finalStageSpawnsCompleted;
        private static bool _stageSpawnsCompleted;
        private static int _remainingStageSeconds;
        private static bool _hasWon;
        private static bool _hasLost;
        private static bool _init;

        public static ArticleFactory ArticleFactory => _articleFactory;

        public static void Init(Map map, float formationGap, List<ArticleObj> selectedArticles,
                                      Action<StageModel> startStage, Action<int> stageTimeChanged,
                                      Action completeVictory, Func<bool, bool> stageTimeExpired, Action monsterDied) {
            if (_init) {
                Dispose();
            }

            _map = map;
            _formationGap = formationGap;
            _selectedArticles = selectedArticles ?? throw new ArgumentNullException(nameof(selectedArticles));
            _startStage = startStage ?? throw new ArgumentNullException(nameof(startStage));
            _stageTimeChanged = stageTimeChanged ?? throw new ArgumentNullException(nameof(stageTimeChanged));
            _completeVictory = completeVictory ?? throw new ArgumentNullException(nameof(completeVictory));
            _stageTimeExpired = stageTimeExpired ?? throw new ArgumentNullException(nameof(stageTimeExpired));
            _monsterDied = monsterDied ?? throw new ArgumentNullException(nameof(monsterDied));
            _articleFactory = new ArticleFactory();
            _monsterFactory = new MonsterFactory();
            _finalStageSpawnsCompleted = false;
            _stageSpawnsCompleted = false;
            _remainingStageSeconds = 0;
            _currentStageMonsters.Clear();
            _hasWon = false;
            _hasLost = false;
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

            Collider2D[] hits = Physics2D.OverlapPointAll(position, ArticleSelectionLayerMask);
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
            Collider2D[] hits = Physics2D.OverlapAreaAll(min, max, ArticleSelectionLayerMask);
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

            _selectedArticles.RemoveAll(article => article == null || !article.gameObject.activeInHierarchy);

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

            for (int i = 0; i < count; i++) {
                if (_selectedArticles[i].MoveComponent is not MoveComponentDestination movement) {
                    continue;
                }

                int row = i / columns;
                int column = i % columns;
                Vector2 offset = new Vector2((column - (columns - 1) * 0.5f) * stepX,
                                             ((rows - 1) * 0.5f - row) * stepY);
                movement.SetDestination(destination + offset);
            }
        }

        public static int AliveMonsterCount {
            get {
                _aliveMonsters.RemoveAll(monster => monster == null || !monster.isActiveAndEnabled || !monster.IsAlive);
                return _aliveMonsters.Count;
            }
        }

        /// <summary>스테이지마다 몬스터 생성과 제한 시간을 함께 진행하고 종료 조건을 처리한다.</summary>
        public static async UniTask RunAsync(CancellationToken cancellationToken) {
            if (!_init) {
                throw new InvalidOperationException("StageLogic is not initialized.");
            }

            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _stageSequenceCancellation = cancellation;
            CancellationToken stageToken = cancellation.Token;
            try {
                for (int stageId = 1; !_hasWon && !_hasLost; stageId++) {
                    stageToken.ThrowIfCancellationRequested();
                    StageModel stage = StageDataSet.GetModel(stageId);
                    if (stage == null) {
                        if (stageId == 1) {
                            Debug.LogError($"[Battle] Stage {stageId} is not loaded; battle cannot start.");
                        } else {
                            TryCompleteVictory();
                        }
                        return;
                    }

                    float stageTime = stage.StageTime;
                    if (stageTime <= 0f) {
                        Debug.LogError($"[Battle] Stage {stageId} has invalid duration: {stageTime} seconds.");
                        return;
                    }
                    int monsterCount = stage.MonsterCount;
                    if (monsterCount <= 0) {
                        Debug.LogError($"[Battle] Stage {stageId} has invalid monster count: {monsterCount}.");
                        return;
                    }
                    if (stage.MonsterHealth <= 0f) {
                        Debug.LogError($"[Battle] Stage {stageId} has invalid monster health: {stage.MonsterHealth}.");
                        return;
                    }
                    if (_map == null) {
                        Debug.LogError("[Battle] Map is missing; monsters cannot spawn.");
                        return;
                    }
                    MonsterModel monsterModel = MonsterDataSet.GetModel(stage.MonsterId);
                    if (monsterModel == null || monsterModel.Prefab == null) {
                        Debug.LogError($"[Battle] Stage {stageId} has no spawnable monster for ID {stage.MonsterId}.");
                        return;
                    }
                    MonsterModel bossModel = null;
                    if (stage.BossMonsterId != 0) {
                        if (stage.BossMonsterHealth <= 0f) {
                            Debug.LogError($"[Battle] Stage {stageId} has invalid boss health: {stage.BossMonsterHealth}.");
                            return;
                        }
                        bossModel = MonsterDataSet.GetModel(stage.BossMonsterId);
                        if (bossModel == null || bossModel.Prefab == null) {
                            Debug.LogError($"[Battle] Stage {stageId} has no spawnable boss for ID {stage.BossMonsterId}.");
                            return;
                        }
                    }

                    _finalStageSpawnsCompleted = false;
                    _stageSpawnsCompleted = false;
                    _currentStageMonsters.Clear();
                    _remainingStageSeconds = Mathf.CeilToInt(stageTime);
                    bool isFinalStage = StageDataSet.GetModel(stageId + 1) == null;
                    _startStage(stage);
                    await UniTask.WhenAll(
                        SpawnMonstersAsync(stage, monsterModel, bossModel, isFinalStage, stageToken),
                        RunStageTimerAsync(stageTime, stageToken));
                }
            } catch (OperationCanceledException) when (stageToken.IsCancellationRequested) {
                // Battle completion or destruction ended the stage sequence.
            } finally {
                if (_stageSequenceCancellation == cancellation) {
                    _stageSequenceCancellation = null;
                }
            }
        }

        private static async UniTask RunStageTimerAsync(float duration, CancellationToken cancellationToken) {
            float nextInterval = duration - (Mathf.CeilToInt(duration) - 1);

            while (_remainingStageSeconds > 0) {
                await UniTask.Delay(TimeSpan.FromSeconds(nextInterval), cancellationToken: cancellationToken);
                _remainingStageSeconds--;
                _stageTimeChanged(_remainingStageSeconds);
                nextInterval = 1f;
            }

            if (!_hasWon && _stageTimeExpired(_stageSpawnsCompleted)) {
                _hasLost = true;
                _stageSequenceCancellation?.Cancel();
            }
        }

        private static async UniTask SpawnMonstersAsync(StageModel stage, MonsterModel model, MonsterModel bossModel, bool isFinalStage, CancellationToken cancellationToken) {
            for (int i = 0; i < stage.MonsterCount; i++) {
                cancellationToken.ThrowIfCancellationRequested();
                SpawnMonster(model, stage.MonsterHealth);

                if (i + 1 < stage.MonsterCount || bossModel != null) {
                    await UniTask.Delay(MonsterSpawnInterval, cancellationToken: cancellationToken);
                }
            }
            if (bossModel != null) {
                cancellationToken.ThrowIfCancellationRequested();
                SpawnMonster(bossModel, stage.BossMonsterHealth);
            }

            _stageSpawnsCompleted = true;
            if (isFinalStage) {
                _finalStageSpawnsCompleted = true;
                TryCompleteVictory();
            } else {
                ShortenClearedStageWait();
            }
        }

        private static void SpawnMonster(MonsterModel model, float health) {
            MonsterObj monster = _monsterFactory.Create(model, _map, health);
            if (monster.IsAlive) {
                monster.MonsterObserver.AddListener(MonsterEventType.Died, OnMonsterDied);
                monster.Released += OnMonsterReleased;
                _aliveMonsters.Add(monster);
                _currentStageMonsters.Add(monster);
            }
        }

        private static void OnMonsterReleased(MonsterObj monster) {
            monster.MonsterObserver.RemoveListener(MonsterEventType.Died, OnMonsterDied);
            monster.Released -= OnMonsterReleased;
            _aliveMonsters.Remove(monster);
            _currentStageMonsters.Remove(monster);
            ShortenClearedStageWait();
            TryCompleteVictory();
        }

        private static void ShortenClearedStageWait() {
            if (!_init || !_stageSpawnsCompleted || _finalStageSpawnsCompleted ||
                _currentStageMonsters.Count != 0 || _remainingStageSeconds <= ClearedStageWaitSeconds) {
                return;
            }

            _remainingStageSeconds = ClearedStageWaitSeconds;
            _stageTimeChanged(_remainingStageSeconds);
        }

        private static void OnMonsterDied() => _monsterDied?.Invoke();

        private static void TryCompleteVictory() {
            if (!_init || _hasWon || _hasLost || !_finalStageSpawnsCompleted || AliveMonsterCount != 0) {
                return;
            }

            _hasWon = true;
            _stageSequenceCancellation?.Cancel();
            _completeVictory();
        }

        public static void Dispose() {
            if (!_init) {
                return;
            }

            _init = false;
            _stageSequenceCancellation?.Cancel();
            foreach (MonsterObj monster in _aliveMonsters) {
                if (monster != null) {
                    monster.MonsterObserver.RemoveListener(MonsterEventType.Died, OnMonsterDied);
                    monster.Released -= OnMonsterReleased;
                }
            }
            _aliveMonsters.Clear();
            _currentStageMonsters.Clear();
            _stageSpawnsCompleted = false;
            _remainingStageSeconds = 0;
            _stageSequenceCancellation = null;
            _selectedArticles = null;
            _startStage = null;
            _stageTimeChanged = null;
            _completeVictory = null;
            _stageTimeExpired = null;
            _monsterDied = null;
            _articleFactory = null;
            _monsterFactory = null;
            _map = null;
        }
    }
}
