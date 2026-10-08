using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Battle.UI;
using LARD.Data;
using LARD.Stage;
using LARD.Upgrade;
using LSJ.UI;
using LSJ.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LARD.Battle {
	[RequireComponent(typeof(GameInputController), typeof(BattleInputLogic))]
	public sealed class BattleManager : Singleton<BattleManager> {
		private const string UiScenePath = "Assets/02Scenes/UI.unity";

		[SerializeField, Min(0f)] private float _formationGap = 0.1f;
		[SerializeField] private Map _map;

		private readonly List<ArticleObj> _selectedArticles = new();
		private GameInputController _input;
		private BattleInputLogic _inputLogic;
		private StageLogic _stageLogic;
		private ArticleLogic _articleLogic;
		private BattleLogic _battleLogic;

		public IReadOnlyList<ArticleObj> SelectedArticles => _selectedArticles;
		public Observer<BattleEventType> BattleObserver { get; } = new();
		public int MonsterKillCount { get; private set; }
		public StageModel CurrentStage { get; private set; }
		public int RemainingStageSeconds { get; private set; }
		public BattleStateType State { get; private set; } = BattleStateType.Preparing;
		public event Action<StageModel> StageChanged;
		public event Action<int> StageTimeChanged;
		public event Action VictoryAchieved;
		public int AliveMonsterCount => _stageLogic?.AliveMonsterCount ?? 0;

		protected override void Awake() {
			base.Awake();
			if (Instance != this) {
				return;
			}

			_input = GetComponent<GameInputController>();
			_inputLogic = GetComponent<BattleInputLogic>();
			_inputLogic.Initialize(this);
			_map ??= FindFirstObjectByType<Map>();
			_stageLogic = new StageLogic(_map, _formationGap, _selectedArticles, StartStage, SetRemainingStageSeconds, CompleteVictory,
			                            () => BattleObserver.TriggerEvent(BattleEventType.MonsterDied));
			_articleLogic = new ArticleLogic(_selectedArticles, _stageLogic.ArticleFactory);
			_battleLogic = new BattleLogic(this);
		}

		private void Start() {
			if (Instance != this) {
				return;
			}

			BattleObserver.AddListener(BattleEventType.MonsterDied, _battleLogic.OnMonsterDied);
			StartStagesAsync().Forget();
		}

		private async UniTask StartStagesAsync() {
			var cancellationToken = this.GetCancellationTokenOnDestroy();
			try {
				if (!SceneManager.GetSceneByPath(UiScenePath).isLoaded) {
					await SceneManager.LoadSceneAsync(UiScenePath, LoadSceneMode.Additive)
						.ToUniTask(cancellationToken: cancellationToken);
				}
				if (cancellationToken.IsCancellationRequested) {
					return;
				}
				await DataManager.Init();
				if (cancellationToken.IsCancellationRequested) {
					return;
				}
				if (UIManager.Instance == null) {
					Debug.LogError("[Battle] UIManager is missing from the UI scene.", this);
				} else {
					await UIManager.Instance.ShowAsync(UIType.Battle);
					if (UIManager.Instance.GetUI<UIBattleMain>(UIType.Battle) == null) {
						Debug.LogError("[Battle] UIBattle prefab could not be shown.", this);
					}
				}
				if (cancellationToken.IsCancellationRequested) {
					return;
				}
				await _stageLogic.RunAsync(cancellationToken);
			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
				// The battle object was destroyed while loading its data.
			}
		}

		private void StartStage(StageModel stage) {
			CurrentStage = stage;
			State = BattleStateType.Playing;
			SetRemainingStageSeconds(Mathf.CeilToInt(stage.StageTime));
			StageChanged?.Invoke(stage);
			Debug.Log($"[Battle] Stage {stage.Id} started ({stage.StageTime} seconds, monster {stage.MonsterId} x{stage.MonsterCount}).", this);
		}

		private void SetRemainingStageSeconds(int remainingSeconds) {
			RemainingStageSeconds = remainingSeconds;
			StageTimeChanged?.Invoke(remainingSeconds);
		}

		private void CompleteVictory() {
			State = BattleStateType.Victory;
			SetRemainingStageSeconds(0);
			Debug.Log("[Battle] Victory! All monsters have been defeated after the final stage spawned.", this);
			VictoryAchieved?.Invoke();
		}

		private void OnEnable() {
			if (Instance != this) {
				return;
			}

			_input.AddSelectPressedListener(_inputLogic.OnSelectPressed);
			_input.AddSelectReleasedListener(_inputLogic.OnSelectReleased);
			_input.AddMoveListener(_inputLogic.OnMoveCommand);
			_input.AddDrawArticleListener(SummonArticle);
			_input.AddUpgradeArticleListener(OnTestUpgradeSelectedArticle);
			_input.AddUpgradeYozListener(OnTestUpgradeYoz);
			Debug.Log("[BattleInput] BattleManager registered input listeners.", this);
		}

		private void OnDisable() {
			if (Instance != this) {
				return;
			}

			_input.RemoveSelectPressedListener(_inputLogic.OnSelectPressed);
			_input.RemoveSelectReleasedListener(_inputLogic.OnSelectReleased);
			_input.RemoveMoveListener(_inputLogic.OnMoveCommand);
			_input.RemoveDrawArticleListener(SummonArticle);
			_input.RemoveUpgradeArticleListener(OnTestUpgradeSelectedArticle);
			_input.RemoveUpgradeYozListener(OnTestUpgradeYoz);
			_inputLogic.CancelDrag();
		}

		protected override void OnDestroy() {
			if (_battleLogic != null) {
				BattleObserver.RemoveListener(BattleEventType.MonsterDied, _battleLogic.OnMonsterDied);
			}
			_stageLogic?.Dispose();
			base.OnDestroy();
		}

		internal int RegisterMonsterKill() => ++MonsterKillCount;

		public void SummonArticle() {
			_stageLogic.SummonArticle();
		}

		public void SelectAt(Vector2 position) {
			_stageLogic.SelectAt(position);
		}

		public void SelectArea(Vector2 start, Vector2 end) {
			_stageLogic.SelectArea(start, end);
		}

		public void MoveSelectedArticles(Vector2 destination) {
			_stageLogic.MoveSelectedArticles(destination);
		}

		public bool TryUpgradeSelectedArticle() {
			return _articleLogic.TryUpgradeSelectedArticle();
		}

		private void OnTestUpgradeSelectedArticle() {
			if (TryUpgradeSelectedArticle()) {
				Debug.Log($"[BattleInput] Upgraded selected article to tier {_selectedArticles[0].Tier}.", _selectedArticles[0]);
			} else {
				Debug.LogWarning("[BattleInput] Article upgrade failed. Check selection, Silver, tier data, and upgrade chance.", this);
			}
		}

		private void OnTestUpgradeYoz() {
			if (UpgradeLogic.TryUpgrade(ArticleRace.Yoz)) {
				PlayerUpgrade upgrade = UpgradeDataSet.GetPlayerUpgrade(ArticleRace.Yoz);
				Debug.Log($"[BattleInput] Yoz upgraded to level {upgrade.Level}.", this);
			} else {
				Debug.LogWarning("[BattleInput] Yoz upgrade failed. Check Gold, level, and upgrade data.", this);
			}
		}
	}
}
