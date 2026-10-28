using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Data;
using LARD.Stage;
using LSJ.UI;
using LSJ.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LARD.Battle {
	/// <summary>
	/// 전투 준비와 진행 상태를 조율하고 입력, 스테이지 로직과 UI 이벤트를 연결한다.
	/// </summary>
	[RequireComponent(typeof(GameInputController), typeof(BattleInputLogic))]
	public sealed class BattleManager : Singleton<BattleManager> {
		private const string BattleScenePath = "Assets/02Scenes/Battle.unity";
		private const string UiScenePath = "Assets/02Scenes/UI.unity";

		[SerializeField, Min(0f)] private float _formationGap = 0.1f;
		[SerializeField] private Map _map;

		private readonly List<ArticleObj> _selectedArticles = new();
		private GameInputController _input;
		private BattleInputLogic _inputLogic;
		private ArticleLogic _articleLogic;
		private BattleLogic _battleLogic;
		private bool _battleStartRequested;
		private bool _restartRequested;
		private bool _resultShown;

		public IReadOnlyList<ArticleObj> SelectedArticles => _selectedArticles;
		public ArticleObj SelectedArticle => _selectedArticles.Count == 1 && _selectedArticles[0] != null &&
		                                    _selectedArticles[0].gameObject.activeInHierarchy && !_selectedArticles[0].IsInPool
			? _selectedArticles[0] : null;
		public Observer<BattleEventType> BattleObserver { get; } = new();
		public int MonsterKillCount { get; private set; }
		public StageModel CurrentStage { get; private set; }
		public int RemainingStageSeconds { get; private set; }
		public BattleStateType State { get; private set; } = BattleStateType.Preparing;
		public bool IsReady { get; private set; }
		public bool HasBattleStarted => _battleStartRequested;
		public event Action<StageModel> StageChanged;
		public event Action<int> StageTimeChanged;
		public event Action<ArticleObj> SelectionChanged;
		public event Action VictoryAchieved;
		public event Action DefeatAchieved;
		public int AliveMonsterCount => StageLogic.AliveMonsterCount;

		// 전투 로직 초기화
		protected override void Awake() {
			base.Awake();
			if (Instance != this) {
				return;
			}

			_input = GetComponent<GameInputController>();
			_inputLogic = GetComponent<BattleInputLogic>();
			_inputLogic.Init(this);
			_map ??= FindFirstObjectByType<Map>();
			_battleLogic = new BattleLogic(this);
			StageLogic.Init(_map, _formationGap, _selectedArticles, StartStage, SetRemainingStageSeconds, CompleteVictory,
			                      _battleLogic.OnStageTimeExpired, () => BattleObserver.TriggerEvent(BattleEventType.MonsterDied));
			_articleLogic = new ArticleLogic(_selectedArticles, StageLogic.ArticleFactory);
			BattleLog.Clear();
		}

		// 전투 시작
		private void Start() {
			if (Instance != this) {
				return;
			}

			BattleObserver.AddListener(BattleEventType.MonsterDied, _battleLogic.OnMonsterDied);
			StartStagesAsync().Forget();
		}

		/// <summary>UI와 전투 데이터를 준비한 뒤 시작 요청을 기다려 스테이지 진행을 실행한다.</summary>
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
					return;
				}
				await UIManager.Instance.ShowAsync(UIType.Battle);
				if (UIManager.Instance.GetUI<UIBattleMain>(UIType.Battle) == null) {
					Debug.LogError("[Battle] UIBattle prefab could not be shown.", this);
					return;
				}
				if (cancellationToken.IsCancellationRequested) {
					return;
				}
				IsReady = true;
				await UniTask.WaitUntil(() => _battleStartRequested, cancellationToken: cancellationToken);
			await StageLogic.RunAsync(cancellationToken);
			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
				// The battle object was destroyed while loading its data.
			}
		}

		/// <summary>전투 준비가 끝났을 때 첫 스테이지 시작을 요청한다.</summary>
		public bool TryStartBattle() {
			if (!IsReady || _battleStartRequested || State != BattleStateType.Preparing) {
				return false;
			}

			_battleStartRequested = true;
			return true;
		}

		public bool TryRestartBattle() {
			if (!IsReady || _restartRequested || !gameObject.activeInHierarchy) {
				return false;
			}

			_restartRequested = true;
			RestartBattleAsync().Forget();
			return true;
		}

		private async UniTask RestartBattleAsync() {
			try {
				await SceneManager.LoadSceneAsync(BattleScenePath, LoadSceneMode.Single).ToUniTask();
			} catch (Exception exception) {
				_restartRequested = false;
				Debug.LogException(exception, this);
			}
		}

		/// <summary>현재 스테이지와 제한 시간을 설정하고 스테이지 변경을 알린다.</summary>
		private void StartStage(StageModel stage) {
			CurrentStage = stage;
			State = BattleStateType.Playing;
			float stageTime = stage.StageTime;
			SetRemainingStageSeconds(Mathf.CeilToInt(stageTime));
			StageChanged?.Invoke(stage);
			Debug.Log($"[Battle] Stage {stage.Id} started ({stageTime} seconds, monster {stage.MonsterId} x{stage.MonsterCount}).", this);
		}

		// 남은 시간 갱신
		private void SetRemainingStageSeconds(int remainingSeconds) {
			RemainingStageSeconds = remainingSeconds;
			StageTimeChanged?.Invoke(remainingSeconds);
		}

		/// <summary>전투를 승리 상태로 전환하고 결과를 알린다.</summary>
		private void CompleteVictory() {
			State = BattleStateType.Victory;
			SetRemainingStageSeconds(0);
			Debug.Log("[Battle] Victory! All monsters have been defeated after the final stage spawned.", this);
			VictoryAchieved?.Invoke();
			ShowBattleResult(true);
		}

		/// <summary>진행 중인 전투를 패배 상태로 전환하고 결과를 알린다.</summary>
		internal void CompleteDefeat() {
			if (State != BattleStateType.Playing) {
				return;
			}

			State = BattleStateType.Defeat;
			SelectionChanged?.Invoke(null);
			Debug.Log("[Battle] Defeat! The stage timer expired before all monsters were defeated.", this);
			DefeatAchieved?.Invoke();
			ShowBattleResult(false);
		}

		private void ShowBattleResult(bool isWin) {
			if (_resultShown) return;

			UIManager manager = UIManager.Instance;
			if (manager == null) {
				Debug.LogError("[Battle] UIManager is missing; battle result could not be shown.", this);
				return;
			}

			_resultShown = true;
			manager.Show(UIType.BattleResult, UIBattleResult.MakeParam(isWin));
		}

		// 전투 입력 등록
		private void OnEnable() {
			if (Instance != this) {
				return;
			}

			_input.AddSelectPressedListener(_inputLogic.OnSelectPressed);
			_input.AddSelectReleasedListener(_inputLogic.OnSelectReleased);
			_input.AddMoveListener(_inputLogic.OnMoveCommand);
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
		public bool CanUpgradeSelectedArticles(out string reason) =>
			_articleLogic.CanUpgradeSelectedArticles(out reason);

		public bool TryUpgradeSelectedArticles() {
			bool upgraded = _articleLogic.TryUpgradeSelectedArticles();
			SelectionChanged?.Invoke(SelectedArticle);
			return upgraded;
		}

		public bool CanUpgradeAvailableArticles(int targetTier, ArticleRace? race) =>
			_articleLogic.CanUpgradeAvailableArticles(targetTier, race);

		public bool TryUpgradeAvailableArticles(int targetTier, ArticleRace? race) {
			bool upgraded = _articleLogic.TryUpgradeAvailableArticles(targetTier, race);
			if (upgraded) SelectionChanged?.Invoke(SelectedArticle);
			return upgraded;
		}

		public bool TrySellSelectedArticle() {
			if (!_articleLogic.TrySellSelectedArticle()) {
				return false;
			}

			SelectionChanged?.Invoke(SelectedArticle);
			return true;
		}
	}
}
