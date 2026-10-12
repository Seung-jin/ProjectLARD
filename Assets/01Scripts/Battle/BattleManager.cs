using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Article;
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
		private ArticleLogic _articleLogic;
		private BattleLogic _battleLogic;

		public IReadOnlyList<ArticleObj> SelectedArticles => _selectedArticles;
		public ArticleObj SelectedArticle => _selectedArticles.Count == 1 && _selectedArticles[0] != null &&
		                                    _selectedArticles[0].gameObject.activeInHierarchy && !_selectedArticles[0].IsInPool
			? _selectedArticles[0] : null;
		public Observer<BattleEventType> BattleObserver { get; } = new();
		public int MonsterKillCount { get; private set; }
		public StageModel CurrentStage { get; private set; }
		public int RemainingStageSeconds { get; private set; }
		public BattleStateType State { get; private set; } = BattleStateType.Preparing;
		public event Action<StageModel> StageChanged;
		public event Action<int> StageTimeChanged;
		public event Action<ArticleObj> SelectionChanged;
		public event Action VictoryAchieved;
		public int AliveMonsterCount => StageLogic.AliveMonsterCount;

		// 전투 로직 초기화
		protected override void Awake() {
			base.Awake();
			if (Instance != this) {
				return;
			}

			_input = GetComponent<GameInputController>();
			_inputLogic = GetComponent<BattleInputLogic>();
			_inputLogic.Initialize(this);
			_map ??= FindFirstObjectByType<Map>();
			StageLogic.Initialize(_map, _formationGap, _selectedArticles, StartStage, SetRemainingStageSeconds, CompleteVictory,
			                      () => BattleObserver.TriggerEvent(BattleEventType.MonsterDied));
			_articleLogic = new ArticleLogic(_selectedArticles, StageLogic.ArticleFactory);
			_battleLogic = new BattleLogic(this);
		}

		// 전투 시작
		private void Start() {
			if (Instance != this) {
				return;
			}

			BattleObserver.AddListener(BattleEventType.MonsterDied, _battleLogic.OnMonsterDied);
			StartStagesAsync().Forget();
		}

		// 전투 준비 및 스테이지 진행
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
			await StageLogic.RunAsync(cancellationToken);
			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
				// The battle object was destroyed while loading its data.
			}
		}

		// 스테이지 시작 처리
		private void StartStage(StageModel stage) {
			CurrentStage = stage;
			State = BattleStateType.Playing;
			float stageTime = StageDataSet.GetStageTime();
			SetRemainingStageSeconds(Mathf.CeilToInt(stageTime));
			StageChanged?.Invoke(stage);
			Debug.Log($"[Battle] Stage {stage.Id} started ({stageTime} seconds, monster {stage.MonsterId} x{StageDataSet.GetMonsterCount()}).", this);
		}

		// 남은 시간 갱신
		private void SetRemainingStageSeconds(int remainingSeconds) {
			RemainingStageSeconds = remainingSeconds;
			StageTimeChanged?.Invoke(remainingSeconds);
		}

		// 승리 처리 진행
		private void CompleteVictory() {
			State = BattleStateType.Victory;
			SetRemainingStageSeconds(0);
			Debug.Log("[Battle] Victory! All monsters have been defeated after the final stage spawned.", this);
			VictoryAchieved?.Invoke();
		}

		// 전투 입력 등록
		private void OnEnable() {
			if (Instance != this) {
				return;
			}

			_input.AddSelectPressedListener(_inputLogic.OnSelectPressed);
			_input.AddSelectReleasedListener(_inputLogic.OnSelectReleased);
			_input.AddMoveListener(_inputLogic.OnMoveCommand);
			_input.AddUpgradeArticleListener(OnTestUpgradeSelectedArticle);
			_input.AddUpgradeYozListener(OnTestUpgradeYoz);
			Debug.Log("[BattleInput] BattleManager registered input listeners.", this);
		}

		// 전투 입력 해제
		private void OnDisable() {
			if (Instance != this) {
				return;
			}

			_input.RemoveSelectPressedListener(_inputLogic.OnSelectPressed);
			_input.RemoveSelectReleasedListener(_inputLogic.OnSelectReleased);
			_input.RemoveMoveListener(_inputLogic.OnMoveCommand);
			_input.RemoveUpgradeArticleListener(OnTestUpgradeSelectedArticle);
			_input.RemoveUpgradeYozListener(OnTestUpgradeYoz);
			_inputLogic.CancelDrag();
		}

		// 전투 리소스 정리
		protected override void OnDestroy() {
			if (_battleLogic != null) {
				BattleObserver.RemoveListener(BattleEventType.MonsterDied, _battleLogic.OnMonsterDied);
			}
			if (Instance == this) {
				StageLogic.Dispose();
			}
			base.OnDestroy();
		}

		// 몬스터 처치 수 갱신
		internal int RegisterMonsterKill() => ++MonsterKillCount;

		// 위치의 유닛 선택
		public void SelectAt(Vector2 position) {
			StageLogic.SelectAt(position);
			SelectionChanged?.Invoke(SelectedArticle);
		}

		// 범위의 유닛 선택
		public void SelectArea(Vector2 start, Vector2 end) {
			StageLogic.SelectArea(start, end);
			SelectionChanged?.Invoke(SelectedArticle);
		}

		// 선택 유닛 이동
		public void MoveSelectedArticles(Vector2 destination) {
			StageLogic.MoveSelectedArticles(destination);
		}

		// 선택 유닛 강화 시도
		public bool TryUpgradeSelectedArticle() {
			bool upgraded = _articleLogic.TryUpgradeSelectedArticle();
			SelectionChanged?.Invoke(SelectedArticle);
			return upgraded;
		}

		private void OnTestUpgradeSelectedArticle() {
			TryUpgradeSelectedArticle();
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
