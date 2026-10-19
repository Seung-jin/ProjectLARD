using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LARD.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LARD.Battle.Editor {
    public sealed class AutoBattleTestWindow : EditorWindow {
        private static readonly string[] StrategyLabels = { "소환만", "2티어 승급", "3티어 승급 (단계 운영)", "1~9스테이지 2티어 → 10스테이지부터 3티어" };
        private int _runs = 10;
        private int _baseSeed = 1000;
        private float _timeScale = 10f;
        private AutoBattleStrategy _strategy = AutoBattleStrategy.PromoteToTier3;

        [MenuItem("Tools/LARD/자동 전투 테스트")]
        private static void Open() => GetWindow<AutoBattleTestWindow>("자동 전투 테스트");

        private void OnGUI() {
            EditorGUILayout.HelpBox(
                "Battle 씬에서 실제 상점과 전투 로직으로 테스트합니다. " +
                "기물 3기로 시작하며, 선택한 방식으로 실버를 사용합니다. " +
                "골드는 두 종족을 번갈아 강화합니다. 3티어 승급 전략은 3스테이지 종료 시 종족을 선택합니다. " +
                "매 실행마다 전투를 재시작하며 결과는 AutoBattleResults에 저장됩니다.",
                MessageType.Info);
            _runs = EditorGUILayout.IntSlider("실행 횟수", _runs, 1, 100);
            _baseSeed = EditorGUILayout.IntField("기본 시드", _baseSeed);
            _timeScale = EditorGUILayout.Slider("진행 배속", _timeScale, 1f, 30f);
            _strategy = (AutoBattleStrategy)EditorGUILayout.Popup("실버 운영 방식", (int)_strategy, StrategyLabels);
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || AutoBattleTestBatch.IsRunning)) {
                if (GUILayout.Button("자동 전투 테스트 실행", GUILayout.Height(32)))
                    AutoBattleTestBatch.Start(_runs, _baseSeed, _timeScale, _strategy);
            }
            if (AutoBattleTestBatch.IsRunning)
                EditorGUILayout.LabelField("진행 중", AutoBattleTestBatch.Progress);

            string resultDirectory = AutoBattleTestBatch.LatestResultDirectory;
            using (new EditorGUI.DisabledScope(!Directory.Exists(resultDirectory))) {
                if (GUILayout.Button("최근 결과 폴더 열기"))
                    EditorUtility.RevealInFinder(resultDirectory);
            }
        }
    }

    [InitializeOnLoad]
    public static class AutoBattleTestBatch {
        private const string ScenePath = "Assets/02Scenes/Battle.unity";
        private const string Prefix = "LARD.AutoBattleTest.";
        private const string ActiveKey = Prefix + "Active";
        private const string RunKey = Prefix + "Run";
        private const string CountKey = Prefix + "Count";
        private const string SeedKey = Prefix + "Seed";
        private const string SpeedKey = Prefix + "Speed";
        private const string StrategyKey = Prefix + "Strategy";
        private const string DirectoryKey = Prefix + "Directory";
        private const string FastPlayModeKey = Prefix + "FastPlayMode";
        private static string LastDirectoryKey => Prefix + "LastDirectory." + Application.dataPath;

        public static bool IsRunning => SessionState.GetBool(ActiveKey, false);
        public static string Progress => $"{SessionState.GetInt(RunKey, 0) + 1} / {SessionState.GetInt(CountKey, 0)}";
        public static string LatestResultDirectory {
            get {
                string latest = EditorPrefs.GetString(LastDirectoryKey, "");
                if (Directory.Exists(latest)) return latest;
                string resultsRoot = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                    "AutoBattleResults");
                return Directory.Exists(resultsRoot) ? resultsRoot : "";
            }
        }

        static AutoBattleTestBatch() {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public static void Start(int runs, int baseSeed, float timeScale, AutoBattleStrategy strategy) {
            if (IsRunning || EditorApplication.isPlayingOrWillChangePlaymode) return;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            if (!File.Exists(Path.Combine(projectRoot, ScenePath)))
                throw new FileNotFoundException("Battle scene was not found.", ScenePath);
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string directory = Path.Combine(projectRoot,
                "AutoBattleResults", DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(directory);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(RunKey, 0);
            SessionState.SetInt(CountKey, Mathf.Max(1, runs));
            SessionState.SetInt(SeedKey, baseSeed);
            SessionState.SetFloat(SpeedKey, Mathf.Clamp(timeScale, 1f, 30f));
            SessionState.SetInt(StrategyKey, (int)strategy);
            SessionState.SetString(DirectoryKey, directory);
            EditorPrefs.SetString(LastDirectoryKey, directory);
            SessionState.SetBool(FastPlayModeKey, EditorSettings.enterPlayModeOptionsEnabled);
            EditorSettings.enterPlayModeOptionsEnabled = false;
            Debug.Log($"[AutoBattleTest] Starting {runs} runs. Results: {directory}");
            EditorApplication.isPlaying = true;
        }

        // Batch mode: Unity.exe -batchmode -nographics -projectPath <project> \
        //   -executeMethod LARD.Battle.Editor.AutoBattleTestBatch.RunFromCommandLine \
        //   -autoRuns 10 -autoSeed 1000 -autoSpeed 10 -autoStrategy PromoteToTier3
        // Do not pass -quit: this method enters Play mode and exits after the last run.
        public static void RunFromCommandLine() {
            string[] args = Environment.GetCommandLineArgs();
            int runs = ParseInt(args, "-autoRuns", 10);
            int seed = ParseInt(args, "-autoSeed", 1000);
            float speed = ParseFloat(args, "-autoSpeed", 10f);
            string strategyText = ParseString(args, "-autoStrategy", "PromoteToTier3");
            if (!Enum.TryParse(strategyText, true, out AutoBattleStrategy strategy))
                throw new ArgumentException($"Unknown auto battle strategy: {strategyText}");
            Start(runs, seed, speed, strategy);
        }

        private static void OnPlayModeChanged(PlayModeStateChange state) {
            if (!IsRunning) return;
            if (state == PlayModeStateChange.EnteredPlayMode) {
                EditorApplication.delayCall += AttachDriver;
            } else if (state == PlayModeStateChange.EnteredEditMode) {
                Debug.LogError("[AutoBattleTest] Play mode ended before the batch finished.");
                StopBatch();
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (IsRunning && EditorApplication.isPlaying && scene.path == ScenePath &&
                SessionState.GetInt(RunKey, 0) > 0)
                EditorApplication.delayCall += AttachDriver;
        }

        private static void AttachDriver() {
            if (!IsRunning || !EditorApplication.isPlaying) return;
            if (UnityEngine.Object.FindAnyObjectByType<AutoBattleTestDriver>() != null) return;
            int run = SessionState.GetInt(RunKey, 0);
            string file = RunPath(run);
            try {
                GameObject holder = new GameObject("Auto Battle Test Driver");
                AutoBattleTestDriver driver = holder.AddComponent<AutoBattleTestDriver>();
                driver.Completed += _ => EditorApplication.delayCall += ContinueOrFinish;
                driver.Configure(SessionState.GetInt(SeedKey, 1000) + run,
                    (AutoBattleStrategy)SessionState.GetInt(StrategyKey, 0),
                    SessionState.GetFloat(SpeedKey, 10f), file);
            } catch (Exception exception) {
                Debug.LogException(exception);
                File.WriteAllText(file, JsonUtility.ToJson(new AutoBattleRunResult {
                    seed = SessionState.GetInt(SeedKey, 1000) + run,
                    error = exception.ToString(),
                }, true));
                EditorApplication.delayCall += ContinueOrFinish;
            }
        }

        private static void ContinueOrFinish() {
            if (!IsRunning || !EditorApplication.isPlaying) return;
            int run = SessionState.GetInt(RunKey, 0);
            if (!File.Exists(RunPath(run))) {
                Debug.LogError($"[AutoBattleTest] Run {run + 1} ended without a result. Batch stopped.");
                StopBatch();
                EditorApplication.isPlaying = false;
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            AutoBattleRunResult completed = JsonUtility.FromJson<AutoBattleRunResult>(File.ReadAllText(RunPath(run)));
            if (!string.IsNullOrEmpty(completed.error)) {
                Debug.LogError($"[AutoBattleTest] Run {run + 1} failed: {completed.error}");
                SessionState.SetInt(CountKey, run + 1);
                try {
                    WriteSummaries();
                } finally {
                    StopBatch();
                    EditorApplication.isPlaying = false;
                    if (Application.isBatchMode) EditorApplication.Exit(1);
                }
                return;
            }
            run++;
            SessionState.SetInt(RunKey, run);
            if (run < SessionState.GetInt(CountKey, 0)) {
                if (BattleManager.Instance == null || !BattleManager.Instance.TryRestartBattle()) {
                    Debug.LogError("[AutoBattleTest] Could not restart the battle for the next run.");
                    SessionState.SetInt(CountKey, run);
                    try {
                        WriteSummaries();
                    } finally {
                        StopBatch();
                        EditorApplication.isPlaying = false;
                        if (Application.isBatchMode) EditorApplication.Exit(1);
                    }
                }
                return;
            }
            try {
                WriteSummaries();
            } finally {
                StopBatch();
                EditorApplication.isPlaying = false;
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }

        private static void StopBatch() {
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(FastPlayModeKey, false);
            SessionState.SetBool(ActiveKey, false);
        }

        private static void WriteSummaries() {
            string directory = SessionState.GetString(DirectoryKey, "");
            int count = SessionState.GetInt(CountKey, 0);
            List<AutoBattleRunResult> results = new();
            for (int i = 0; i < count; i++)
                results.Add(JsonUtility.FromJson<AutoBattleRunResult>(File.ReadAllText(RunPath(i))));

            List<string> runs = new() {
                "run,seed,strategy,upgradeRace1,upgradeRace2,clearedStage,failedStage,victory,kills,summonAttempts,summons,summonBlocked,promotionAttempts,promotions,raceUpgrades,tier1,tier2,tier3,beastkinLevel,mageLevel,warriorLevel,error"
            };
            List<string> stages = new() {
                "run,stage,cleared,kills,remainingSeconds,summonAttempts,summons,tier1,tier2,tier3,beastkinLevel,mageLevel,warriorLevel,silver,gold"
            };
            for (int i = 0; i < results.Count; i++) {
                AutoBattleRunResult result = results[i];
                AutoBattleStageResult last = result.stages.LastOrDefault();
                runs.Add(string.Join(",", i + 1, result.seed, result.strategy,
                    Csv(result.upgradeRace1), Csv(result.upgradeRace2), result.clearedStage,
                    result.failedStage, result.victory, result.kills, result.summonAttempts, result.summons,
                    result.summonBlocked,
                    result.promotionAttempts, result.promotions, result.raceUpgrades,
                    last?.tier1 ?? 0, last?.tier2 ?? 0, last?.tier3 ?? 0,
                    last?.beastkinLevel ?? 0, last?.mageLevel ?? 0, last?.warriorLevel ?? 0,
                    Csv(result.error)));
                foreach (AutoBattleStageResult stage in result.stages)
                    stages.Add(string.Join(",", i + 1, stage.stage, stage.cleared, stage.kills,
                        stage.remainingSeconds, stage.summonAttempts, stage.summons,
                        stage.tier1, stage.tier2, stage.tier3, stage.beastkinLevel,
                        stage.mageLevel, stage.warriorLevel, stage.silver, stage.gold));
            }
            File.WriteAllLines(Path.Combine(directory, "runs.csv"), runs);
            File.WriteAllLines(Path.Combine(directory, "stages.csv"), stages);

            List<string> rates = new() { "stage,attempts,clears,clearRate,wholeRunClearRate" };
            List<AutoBattleRunResult> valid = results.Where(x => string.IsNullOrEmpty(x.error)).ToList();
            for (int stage = 1; stage <= 25; stage++) {
                int attempts = valid.Count(x => x.stages.Any(y => y.stage == stage));
                int clears = valid.Count(x => x.clearedStage >= stage);
                double stageRate = attempts > 0 ? (double)clears / attempts : 0;
                double wholeRunRate = valid.Count > 0 ? (double)clears / valid.Count : 0;
                rates.Add(string.Join(",", stage, attempts, clears,
                    stageRate.ToString("F4", CultureInfo.InvariantCulture),
                    wholeRunRate.ToString("F4", CultureInfo.InvariantCulture)));
            }
            File.WriteAllLines(Path.Combine(directory, "clear-rates.csv"), rates);
            Debug.Log($"[AutoBattleTest] {count} runs complete. Results: {directory}");
        }

        private static string RunPath(int run) => Path.Combine(SessionState.GetString(DirectoryKey, ""),
            $"run-{run + 1:D3}.json");

        private static string Csv(string value) => "\"" + (value ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";

        private static string ParseString(string[] args, string name, string fallback) {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return fallback;
        }

        private static int ParseInt(string[] args, string name, int fallback) =>
            int.TryParse(ParseString(args, name, ""), out int value) ? value : fallback;

        private static float ParseFloat(string[] args, string name, float fallback) =>
            float.TryParse(ParseString(args, name, ""), NumberStyles.Float,
                CultureInfo.InvariantCulture, out float value) ? value : fallback;
    }
}
