using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Battle;
using LARD.Monster;
using LARD.Shop;
using UnityEngine;

namespace LARD.Stage {
    public sealed class StageLogic : IDisposable {
        private static readonly TimeSpan MonsterSpawnInterval = TimeSpan.FromSeconds(0.5);
        private static readonly int ArticleSelectionLayerMask = LayerMask.GetMask("ArticleSelection");

        private readonly Map _map;
        private readonly float _formationGap;
        private readonly List<ArticleObj> _selectedArticles;
        private readonly Action<StageModel> _startStage;
        private readonly Action<int> _stageTimeChanged;
        private readonly Action _completeVictory;
        private readonly Action _monsterDied;
        private readonly ArticleFactory _articleFactory = new();
        private readonly MonsterFactory _monsterFactory = new();
        private readonly List<MonsterObj> _aliveMonsters = new();

        private CancellationTokenSource _stageSequenceCancellation;
        private bool _finalStageSpawnsCompleted;
        private bool _hasWon;
        private bool _disposed;

        public ArticleFactory ArticleFactory => _articleFactory;

        public StageLogic(Map map, float formationGap, List<ArticleObj> selectedArticles, Action<StageModel> startStage,
                          Action<int> stageTimeChanged, Action completeVictory, Action monsterDied) {
            _map = map;
            _formationGap = formationGap;
            _selectedArticles = selectedArticles ?? throw new ArgumentNullException(nameof(selectedArticles));
            _startStage = startStage ?? throw new ArgumentNullException(nameof(startStage));
            _stageTimeChanged = stageTimeChanged ?? throw new ArgumentNullException(nameof(stageTimeChanged));
            _completeVictory = completeVictory ?? throw new ArgumentNullException(nameof(completeVictory));
            _monsterDied = monsterDied ?? throw new ArgumentNullException(nameof(monsterDied));
        }

        public void Select(ArticleObj article) {
            if (article != null && !_selectedArticles.Contains(article)) {
                _selectedArticles.Add(article);
            }
        }

        public void Deselect(ArticleObj article) {
            _selectedArticles.Remove(article);
        }

        public void ClearSelection() {
            _selectedArticles.Clear();
        }

        public void SelectAt(Vector2 position) {
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

        public void SelectArea(Vector2 start, Vector2 end) {
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

        public bool TryDrawArticle(out ArticleObj article) {
            return ShopLogic.TryDrawArticle(_map, _articleFactory, out article);
        }

        public void SummonArticle() {
            if (TryDrawArticle(out ArticleObj article)) {
                Debug.Log($"[Battle] Summoned article {article.Model.Id}, tier {article.Tier}.", article);
            } else {
                Debug.LogWarning("[Battle] Article summon failed. Check Silver and loaded shop data.");
            }
        }

        public void MoveSelectedArticles(Vector2 destination) {
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

        public int AliveMonsterCount {
            get {
                _aliveMonsters.RemoveAll(monster => monster == null || !monster.isActiveAndEnabled || !monster.IsAlive);
                return _aliveMonsters.Count;
            }
        }

        public async UniTask RunAsync(CancellationToken cancellationToken) {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(StageLogic));
            }

            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _stageSequenceCancellation = cancellation;
            CancellationToken stageToken = cancellation.Token;
            try {
                for (int stageId = 1; !_hasWon; stageId++) {
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

                    if (stage.StageTime <= 0f) {
                        Debug.LogError($"[Battle] Stage {stageId} has invalid duration: {stage.StageTime} seconds.");
                        return;
                    }
                    if (_map == null) {
                        Debug.LogError("[Battle] Map is missing; monsters cannot spawn.");
                        return;
                    }
                    MonsterModel monsterModel = MonsterDataSet.GetModel(stage.MonsterId);
                    if (stage.MonsterCount > 0 && (monsterModel == null || monsterModel.Prefab == null)) {
                        Debug.LogError($"[Battle] Stage {stageId} has no spawnable monster for ID {stage.MonsterId}.");
                        return;
                    }

                    _finalStageSpawnsCompleted = false;
                    bool isFinalStage = StageDataSet.GetModel(stageId + 1) == null;
                    _startStage(stage);
                    await UniTask.WhenAll(
                        SpawnMonstersAsync(stage, monsterModel, isFinalStage, stageToken),
                        RunStageTimerAsync(stage.StageTime, stageToken));
                }
            } catch (OperationCanceledException) when (stageToken.IsCancellationRequested) {
                // Victory or destruction ended the stage sequence.
            } finally {
                if (_stageSequenceCancellation == cancellation) {
                    _stageSequenceCancellation = null;
                }
            }
        }

        private async UniTask RunStageTimerAsync(float duration, CancellationToken cancellationToken) {
            int remainingSeconds = Mathf.CeilToInt(duration);
            float nextInterval = duration - (remainingSeconds - 1);

            while (remainingSeconds > 0) {
                await UniTask.Delay(TimeSpan.FromSeconds(nextInterval), cancellationToken: cancellationToken);
                remainingSeconds--;
                _stageTimeChanged(remainingSeconds);
                nextInterval = 1f;
            }
        }

        private async UniTask SpawnMonstersAsync(StageModel stage, MonsterModel model, bool isFinalStage, CancellationToken cancellationToken) {
            for (int i = 0; i < stage.MonsterCount; i++) {
                cancellationToken.ThrowIfCancellationRequested();
                MonsterObj monster = _monsterFactory.Create(model, _map);
                if (monster.IsAlive) {
                    monster.MonsterObserver.AddListener(MonsterEventType.Died, OnMonsterDied);
                    monster.Released += OnMonsterReleased;
                    _aliveMonsters.Add(monster);
                }

                if (i + 1 < stage.MonsterCount) {
                    await UniTask.Delay(MonsterSpawnInterval, cancellationToken: cancellationToken);
                }
            }

            if (isFinalStage) {
                _finalStageSpawnsCompleted = true;
                TryCompleteVictory();
            }
        }

        private void OnMonsterReleased(MonsterObj monster) {
            monster.MonsterObserver.RemoveListener(MonsterEventType.Died, OnMonsterDied);
            monster.Released -= OnMonsterReleased;
            _aliveMonsters.Remove(monster);
            TryCompleteVictory();
        }

        private void OnMonsterDied() => _monsterDied();

        private void TryCompleteVictory() {
            if (_hasWon || !_finalStageSpawnsCompleted || AliveMonsterCount != 0) {
                return;
            }

            _hasWon = true;
            _stageSequenceCancellation?.Cancel();
            _completeVictory();
        }

        public void Dispose() {
            if (_disposed) {
                return;
            }

            _disposed = true;
            _stageSequenceCancellation?.Cancel();
            foreach (MonsterObj monster in _aliveMonsters) {
                if (monster != null) {
                    monster.MonsterObserver.RemoveListener(MonsterEventType.Died, OnMonsterDied);
                    monster.Released -= OnMonsterReleased;
                }
            }
            _aliveMonsters.Clear();
        }
    }
}
