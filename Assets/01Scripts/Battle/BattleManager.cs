using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Data;
using LARD.Stage;
using UnityEngine;

namespace LARD.Battle {
	[RequireComponent(typeof(GameInputController), typeof(BattleInputLogic))]
	public sealed class BattleManager : MonoBehaviour {
		[SerializeField, Min(0f)] private float _formationGap = 0.1f;
		[SerializeField] private Map _map;

		private readonly List<ArticleObj> _selectedArticles = new();
		private GameInputController _input;
		private BattleInputLogic _inputLogic;
		private StageLogic _stageLogic;

		public IReadOnlyList<ArticleObj> SelectedArticles => _selectedArticles;
		public StageModel CurrentStage { get; private set; }
		public BattleStateType State { get; private set; } = BattleStateType.Preparing;
		public event Action VictoryAchieved;
		public int AliveMonsterCount => _stageLogic?.AliveMonsterCount ?? 0;

		private void Awake() {
			_input = GetComponent<GameInputController>();
			_inputLogic = GetComponent<BattleInputLogic>();
			_inputLogic.Initialize(this);
			_map ??= FindFirstObjectByType<Map>();
			_stageLogic = new StageLogic(_map, _formationGap, _selectedArticles, StartStage, CompleteVictory);
		}

		private void Start() {
			StartStagesAsync().Forget();
		}

		private async UniTask StartStagesAsync() {
			var cancellationToken = this.GetCancellationTokenOnDestroy();
			try {
				await DataManager.Init();
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
			Debug.Log($"[Battle] Stage {stage.Id} started ({stage.StageTime} seconds, monster {stage.MonsterId} x{stage.MonsterCount}).", this);
		}

		private void CompleteVictory() {
			State = BattleStateType.Victory;
			Debug.Log("[Battle] Victory! All monsters have been defeated after the final stage spawned.", this);
			VictoryAchieved?.Invoke();
		}

		private void OnEnable() {
			_input.AddSelectPressedListener(_inputLogic.OnSelectPressed);
			_input.AddSelectReleasedListener(_inputLogic.OnSelectReleased);
			_input.AddMoveListener(_inputLogic.OnMoveCommand);
			_input.AddDrawArticleListener(_stageLogic.OnTestDrawArticle);
			Debug.Log("[BattleInput] BattleManager registered input listeners.", this);
		}

		private void OnDisable() {
			_input.RemoveSelectPressedListener(_inputLogic.OnSelectPressed);
			_input.RemoveSelectReleasedListener(_inputLogic.OnSelectReleased);
			_input.RemoveMoveListener(_inputLogic.OnMoveCommand);
			_input.RemoveDrawArticleListener(_stageLogic.OnTestDrawArticle);
			_inputLogic.CancelDrag();
		}

		private void OnDestroy() {
			_stageLogic?.Dispose();
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
	}
}
