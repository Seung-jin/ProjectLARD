using System;
using System.Collections.Generic;
using System.IO;
using LARD.Article;
using LARD.Player;
using LARD.Shop;
using LARD.Stage;
using LARD.Stat;
using LARD.Upgrade;
using UnityEngine;

namespace LARD.Battle {
    public enum AutoBattleStrategy {
        SummonOnly,
        PromoteToTier2,
        PromoteToTier3,
        PromoteTier2ThenTier3,
        AlwaysPromote,
    }

    [Serializable]
    public sealed class AutoBattleStageResult {
        public int stage;
        public bool cleared;
        public int kills;
        public int remainingSeconds;
        public int summonAttempts;
        public int summons;
        public int tier1;
        public int tier2;
        public int tier3;
        public int beastkinLevel;
        public int mageLevel;
        public int warriorLevel;
        public long silver;
        public long gold;
    }

    [Serializable]
    public sealed class AutoBattleRunResult {
        public int seed;
        public string strategy;
        public float timeScale;
        public bool victory;
        public int clearedStage;
        public int failedStage;
        public int kills;
        public int summonAttempts;
        public int summons;
        public bool summonBlocked;
        public int promotionAttempts;
        public int promotions;
        public int raceUpgrades;
        public string upgradeRace1;
        public string upgradeRace2;
        public string error;
        public List<AutoBattleStageResult> stages = new();
    }

    /// <summary>
    /// 실제 전투 씬에서 지정한 시드와 전략으로 소환·승급·강화를 자동 실행하고 스테이지별 결과를 기록한다.
    /// </summary>
    public sealed class AutoBattleTestDriver : MonoBehaviour {
        private const int ShopId = 1;
        private const int PromotionStrategyArticleTarget = 6;
        private const int Tier3PromotionStartStage = 10;
        private const int Tier3StrategySummonOnlyThroughStage = 3;
        private const int Tier3StrategyAlternatingThroughStage = 5;
        private const int Tier3StrategyPromotionStartStage = 9;
        private const float LoadTimeoutSeconds = 60f;
        private static readonly ArticleRace[] UpgradedRaces = { ArticleRace.BeastSkin, ArticleRace.Mage };

        private BattleManager _battle;
        private AutoBattleStrategy _strategy;
        private AutoBattleRunResult _result;
        private string _outputPath;
        private float _previousTimeScale;
        private float _loadStartedAt;
        private bool _configured;
        private bool _started;
        private bool _finished;
        private bool _summonBlocked;
        private int _currentStage;
        private int _stageStartKills;
        private int _lastStageRemainingSeconds;
        private bool _nextTier2ActionIsPromotion = true;
        private ArticleRace[] _selectedUpgradeRaces;
        private readonly AutoBattlePlacementLogic _placement = new();

        public event Action<AutoBattleRunResult> Completed;

        public void Configure(int seed, AutoBattleStrategy strategy, float timeScale, string outputPath) {
            if (_configured) throw new InvalidOperationException("Auto battle driver is already configured.");
            _battle = BattleManager.Instance;
            if (_battle == null) throw new InvalidOperationException("BattleManager is missing from the scene.");
            _strategy = strategy;
            _outputPath = outputPath;
            _result = new AutoBattleRunResult {
                seed = seed,
                strategy = strategy.ToString(),
                timeScale = timeScale,
            };
            if (strategy != AutoBattleStrategy.PromoteToTier3) {
                _result.upgradeRace1 = UpgradedRaces[0].ToString();
                _result.upgradeRace2 = UpgradedRaces[1].ToString();
            }
            _previousTimeScale = Time.timeScale;
            Time.timeScale = Mathf.Clamp(timeScale, 1f, 30f);
            UnityEngine.Random.InitState(seed);
            _loadStartedAt = Time.realtimeSinceStartup;
            _battle.StageChanged += OnStageChanged;
            _battle.StageTimeChanged += OnStageTimeChanged;
            _battle.VictoryAchieved += OnVictory;
            _battle.DefeatAchieved += OnDefeat;
            _configured = true;
        }

        private void Update() {
            if (!_configured || _finished) return;

            if (!_started) {
                if (!_battle.IsReady || PlayerDataSet.Resource == null || ShopDataSet.GetConstModel(ShopId) == null ||
                    ShopDataSet.GetProbModel(ShopId) == null || StageDataSet.GetModel(1) == null ||
                    ArticleDataSet.GetTierModel(1) == null) {
                    if (Time.realtimeSinceStartup - _loadStartedAt > LoadTimeoutSeconds)
                        Finish("Timed out while loading battle data.");
                    return;
                }

                for (int i = 0; i < 3; i++) {
                    if (!TrySummon()) {
                        Finish("Could not summon the three starting articles.");
                        return;
                    }
                }
                if (!_battle.TryStartBattle()) {
                    Finish("Could not start the battle.");
                    return;
                }
                _started = true;
            }

            SpendGold();
            for (int i = 0; i < 8; i++) {
                if (!SpendSilverOnce()) break;
            }
        }

        private bool SpendSilverOnce() {
            PlayerResource resource = PlayerDataSet.Resource;
            if (resource == null) return false;

            if (_strategy == AutoBattleStrategy.AlwaysPromote) {
                if (TryPromoteCandidate()) return true;
                return TrySummonWithSilver(resource);
            }

            if (_strategy == AutoBattleStrategy.PromoteToTier3) {
                if (_currentStage <= Tier3StrategySummonOnlyThroughStage)
                    return TrySummonWithSilver(resource);
                if (_currentStage <= Tier3StrategyAlternatingThroughStage)
                    return SpendAlternatingTier2Silver(resource);
                if (TryPromoteCandidate()) return true;
                return TrySummonWithSilver(resource);
            }

            if (_strategy != AutoBattleStrategy.SummonOnly && !_summonBlocked &&
                CountActiveArticles() < PromotionStrategyArticleTarget &&
                resource.Silver >= ShopDataSet.GetConstModel(ShopId).PurchaseCost) {
                if (TrySummonWithSilver(resource)) return true;
            }

            if (_strategy != AutoBattleStrategy.SummonOnly && TryPromoteCandidate()) return true;

            return TrySummonWithSilver(resource);
        }

        private bool SpendAlternatingTier2Silver(PlayerResource resource) {
            if (_nextTier2ActionIsPromotion) {
                if (FindPromotionCandidates() == null) {
                    _nextTier2ActionIsPromotion = false;
                } else if (TryPromoteCandidate()) {
                    _nextTier2ActionIsPromotion = false;
                    return true;
                } else {
                    _nextTier2ActionIsPromotion = false;
                }
            }

            if (TrySummonWithSilver(resource)) {
                _nextTier2ActionIsPromotion = true;
                return true;
            }

            return _summonBlocked && TryPromoteCandidate();
        }

        private bool TryPromoteCandidate() {
            List<ArticleObj> candidates = FindPromotionCandidates();
            if (candidates == null) return false;
            int previousId = candidates[0].Id;
            StageLogic.ClearSelection();
            foreach (ArticleObj candidate in candidates) StageLogic.Select(candidate);
            _result.promotionAttempts++;
            bool promoted = _battle.TryUpgradeSelectedArticles();
            if (promoted) {
                _result.promotions++;
                _summonBlocked = false;
                if (_battle.SelectedArticle != null)
                    _placement.Transfer(previousId, _battle.SelectedArticle.Id);
            }
            StageLogic.ClearSelection();
            return promoted;
        }

        private bool TrySummonWithSilver(PlayerResource resource) {
            ShopConstModel shop = ShopDataSet.GetConstModel(ShopId);
            if (_summonBlocked || shop == null || resource.Silver < shop.PurchaseCost) return false;
            if (TrySummon()) return true;
            _summonBlocked = true;
            _result.summonBlocked = true;
            return false;
        }

        private static int CountActiveArticles() {
            int count = 0;
            foreach (ArticleObj article in FindObjectsByType<ArticleObj>()) {
                if (article != null && article.gameObject.activeInHierarchy && !article.IsInPool)
                    count++;
            }
            return count;
        }

        private int GetTargetTier() {
            if (_strategy == AutoBattleStrategy.PromoteToTier2 ||
                (_strategy == AutoBattleStrategy.PromoteTier2ThenTier3 &&
                 _currentStage < Tier3PromotionStartStage) ||
                (_strategy == AutoBattleStrategy.PromoteToTier3 &&
                 _currentStage < Tier3StrategyPromotionStartStage)) return 2;
            return 3;
        }

        private List<ArticleObj> FindPromotionCandidates() {
            int targetTier = GetTargetTier();
            List<ArticleObj> active = new();
            foreach (ArticleObj article in FindObjectsByType<ArticleObj>()) {
                if (article == null || !article.gameObject.activeInHierarchy || article.IsInPool || article.Model == null)
                    continue;
                if (article.Tier < targetTier) active.Add(article);
            }
            active.Sort((left, right) => left.Id.CompareTo(right.Id));

            for (int tier = Mathf.Min(2, targetTier - 1); tier >= 1; tier--) {
                foreach (ArticleRace race in new[] { ArticleRace.BeastSkin, ArticleRace.Mage, ArticleRace.Warrior }) {
                    List<ArticleObj> group = active.FindAll(article => article.Tier == tier && article.Model.Race == race);
                    if (group.Count < 3) continue;
                    List<ArticleObj> candidates = group.GetRange(0, 3);
                    if (ArticleLogic.CanPromoteArticles(candidates, out _)) return candidates;
                }

                List<ArticleObj> mixed = new();
                foreach (ArticleRace race in new[] { ArticleRace.BeastSkin, ArticleRace.Mage, ArticleRace.Warrior }) {
                    ArticleObj article = active.Find(item => item.Tier == tier && item.Model.Race == race);
                    if (article != null) mixed.Add(article);
                }
                if (ArticleLogic.CanPromoteArticles(mixed, out _)) return mixed;
            }
            return null;
        }

        private bool TrySummon() {
            _result.summonAttempts++;
            if (!ShopLogic.TryPickAndSummonArticle()) return false;
            _result.summons++;
            PlaceNewestArticle();
            return true;
        }

        private void PlaceNewestArticle() {
            ArticleObj newest = null;
            foreach (ArticleObj article in FindObjectsByType<ArticleObj>()) {
                if (article == null || !article.gameObject.activeInHierarchy || article.IsInPool) continue;
                if (newest == null || article.Id > newest.Id) newest = article;
            }
            if (newest == null) return;

            Map map = FindAnyObjectByType<Map>();
            _placement.Place(map, newest, _currentStage >= 4);
        }

        private void SpendGold() {
            if (_strategy == AutoBattleStrategy.PromoteToTier3 && _selectedUpgradeRaces == null)
                return;
            ArticleRace[] races = _strategy == AutoBattleStrategy.PromoteToTier3
                ? _selectedUpgradeRaces : UpgradedRaces;
            for (int attempt = 0; attempt < 30; attempt++) {
                ArticleRace chosen = races[0];
                int lowestLevel = int.MaxValue;
                foreach (ArticleRace race in races) {
                    PlayerUpgrade upgrade = UpgradeDataSet.GetPlayerUpgrade(race);
                    if (upgrade != null && upgrade.Level < lowestLevel) {
                        chosen = race;
                        lowestLevel = upgrade.Level;
                    }
                }
                if (lowestLevel == int.MaxValue || !UpgradeLogic.TryUpgrade(chosen)) break;
                _result.raceUpgrades++;
            }
        }

        private void OnStageChanged(StageModel stage) {
            if (_currentStage > 0) CaptureStage(true);
            if (_strategy == AutoBattleStrategy.PromoteToTier3 &&
                _currentStage == Tier3StrategySummonOnlyThroughStage && stage.Id >= 4)
                ChooseUpgradeRaces();
            if (_currentStage < 4 && stage.Id >= 4)
                _placement.Place(FindAnyObjectByType<Map>(), null, true);
            _currentStage = stage.Id;
            _stageStartKills = _battle.MonsterKillCount;
            _lastStageRemainingSeconds = Mathf.CeilToInt(stage.StageTime);
        }

        private void ChooseUpgradeRaces() {
            List<ArticleRace> races = _placement.GetRacesByCount();
            _selectedUpgradeRaces = new[] { races[0], races[1] };
            _result.upgradeRace1 = races[0].ToString();
            _result.upgradeRace2 = races[1].ToString();
            Debug.Log($"[AutoBattleTest] Stage 4 upgrade races by article count: " +
                $"{races[0]} and {races[1]}.");
        }

        private void OnStageTimeChanged(int seconds) {
            if (_battle.CurrentStage != null && _battle.CurrentStage.Id == _currentStage)
                _lastStageRemainingSeconds = seconds;
        }

        private void OnVictory() {
            CaptureStage(true);
            _result.victory = true;
            Finish(null);
        }

        private void OnDefeat() {
            CaptureStage(false);
            _result.failedStage = _currentStage;
            Finish(null);
        }

        private void CaptureStage(bool cleared) {
            if (_currentStage <= 0 || _result.stages.Exists(x => x.stage == _currentStage)) return;
            AutoBattleStageResult stage = new() {
                stage = _currentStage,
                cleared = cleared,
                kills = _battle.MonsterKillCount - _stageStartKills,
                remainingSeconds = _lastStageRemainingSeconds,
                summonAttempts = _result.summonAttempts,
                summons = _result.summons,
                silver = PlayerDataSet.Resource?.Silver ?? 0,
                gold = PlayerDataSet.Resource?.Gold ?? 0,
                beastkinLevel = UpgradeDataSet.GetPlayerUpgrade(ArticleRace.BeastSkin)?.Level ?? 0,
                mageLevel = UpgradeDataSet.GetPlayerUpgrade(ArticleRace.Mage)?.Level ?? 0,
                warriorLevel = UpgradeDataSet.GetPlayerUpgrade(ArticleRace.Warrior)?.Level ?? 0,
            };
            foreach (ArticleObj article in FindObjectsByType<ArticleObj>()) {
                if (article == null || !article.gameObject.activeInHierarchy || article.IsInPool) continue;
                switch (article.Tier) {
                    case 1: stage.tier1++; break;
                    case 2: stage.tier2++; break;
                    case 3: stage.tier3++; break;
                }
            }
            _result.stages.Add(stage);
            if (cleared) _result.clearedStage = _currentStage;
        }

        private void Finish(string error) {
            if (_finished) return;
            _finished = true;
            _result.error = error;
            _result.kills = _battle != null ? _battle.MonsterKillCount : 0;
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(_outputPath) ?? ".");
                File.WriteAllText(_outputPath, JsonUtility.ToJson(_result, true));
                Debug.Log($"[AutoBattleTest] Saved result: {_outputPath}");
            } catch (Exception exception) {
                Debug.LogException(exception);
            }
            Completed?.Invoke(_result);
        }

        private void OnDestroy() {
            Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;
            if (_battle == null) return;
            _battle.StageChanged -= OnStageChanged;
            _battle.StageTimeChanged -= OnStageTimeChanged;
            _battle.VictoryAchieved -= OnVictory;
            _battle.DefeatAchieved -= OnDefeat;
        }
    }
}