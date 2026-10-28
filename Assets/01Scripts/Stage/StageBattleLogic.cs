using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LARD.Battle;
using LARD.Monster;
using UnityEngine;

namespace LARD.Stage {
    /// <summary>스테이지별 몬스터 생성, 제한 시간과 승패 조건을 관리한다.</summary>
    internal static class StageBattleLogic {
        private static readonly TimeSpan MonsterSpawnInterval = TimeSpan.FromSeconds(0.5);
        private const int ClearedStageWaitSeconds = 20;

        private static Map _map;
        private static Action<StageModel> _startStage;
        private static Action<int> _stageTimeChanged;
        private static Action _completeVictory;
        private static Func<bool, bool> _stageTimeExpired;
        private static Action _monsterDied;
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

        public static void Init(Map map, Action<StageModel> startStage, Action<int> stageTimeChanged,
                                Action completeVictory, Func<bool, bool> stageTimeExpired, Action monsterDied) {
            if (_init) {
                Dispose();
            }

            _map = map;
            _startStage = startStage ?? throw new ArgumentNullException(nameof(startStage));
            _stageTimeChanged = stageTimeChanged ?? throw new ArgumentNullException(nameof(stageTimeChanged));
            _completeVictory = completeVictory ?? throw new ArgumentNullException(nameof(completeVictory));
            _stageTimeExpired = stageTimeExpired ?? throw new ArgumentNullException(nameof(stageTimeExpired));
            _monsterDied = monsterDied ?? throw new ArgumentNullException(nameof(monsterDied));
            _monsterFactory = new MonsterFactory();
            _finalStageSpawnsCompleted = false;
            _stageSpawnsCompleted = false;
            _remainingStageSeconds = 0;
            _currentStageMonsters.Clear();
            _hasWon = false;
            _hasLost = false;
            _init = true;
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

            if (!_hasWon) _stageTimeExpired(_stageSpawnsCompleted);
        }

        /// <summary>시간 초과 패배 시 스테이지 진행을 멈추고 남은 몬스터를 반환한다.</summary>
        public static void CompleteDefeat() {
            if (!_init || _hasWon || _hasLost) return;

            _hasLost = true;
            _stageSequenceCancellation?.Cancel();
            foreach (MonsterObj monster in _aliveMonsters.ToArray()) {
                if (monster != null && !monster.IsInPool) _monsterFactory.Release(monster);
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
            _startStage = null;
            _stageTimeChanged = null;
            _completeVictory = null;
            _stageTimeExpired = null;
            _monsterDied = null;
            _monsterFactory = null;
            _map = null;
        }
    }
}
