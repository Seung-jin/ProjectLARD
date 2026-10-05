using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LARD.Article;
using LARD.Data;
using LARD.Monster;
using LARD.Shop;
using LARD.Stage;
using UnityEngine;

namespace LARD.Battle {
	[RequireComponent(typeof(GameInputController), typeof(BattleInputLogic))]
	public sealed class BattleManager : MonoBehaviour {
		private static readonly TimeSpan MonsterSpawnInterval = TimeSpan.FromSeconds(0.5);

		[SerializeField, Min(0f)] private float _formationGap = 0.1f;
		[SerializeField] private Map _map;

		private readonly List<ArticleObj> _selectedArticles = new();
		private readonly List<MonsterObj> _aliveMonsters = new();
		private GameInputController _input;
		private BattleInputLogic _inputLogic;
		private ArticleFactory _articleFactory;
		private MonsterFactory _monsterFactory;

		public IReadOnlyList<ArticleObj> SelectedArticles => _selectedArticles;
		public StageModel CurrentStage { get; private set; }
		public int AliveMonsterCount {
			get {
				_aliveMonsters.RemoveAll(monster => monster == null || !monster.isActiveAndEnabled || !monster.IsAlive);
				return _aliveMonsters.Count;
			}
		}

		private void Awake() {
			_input = GetComponent<GameInputController>();
			_inputLogic = GetComponent<BattleInputLogic>();
			_inputLogic.Initialize(this);
			_map ??= FindFirstObjectByType<Map>();
			_articleFactory = new ArticleFactory();
			_monsterFactory = new MonsterFactory();
		}

		private void Start() {
			RunStagesAsync().Forget();
		}

		private async UniTask RunStagesAsync() {
			var cancellationToken = this.GetCancellationTokenOnDestroy();
			try {
				await DataManager.Init();
				if (cancellationToken.IsCancellationRequested) {
					return;
				}

				for (int stageId = 1;; stageId++) {
					StageModel stage = StageDataSet.GetModel(stageId);
					if (stage == null) {
						Debug.LogWarning(CurrentStage == null
							                 ? $"[Battle] Stage {stageId} is not loaded; battle cannot start."
							                 : $"[Battle] Stage {stageId} is not available; stage progression stopped after {CurrentStage.Id}.", this);
						return;
					}

					if (stage.StageTime <= 0f) {
						Debug.LogError($"[Battle] Stage {stageId} has invalid duration: {stage.StageTime} seconds.", this);
						return;
					}
					if (_map == null) {
						Debug.LogError("[Battle] Map is missing; monsters cannot spawn.", this);
						return;
					}
					MonsterModel monsterModel = MonsterDataSet.GetModel(stage.MonsterId);
					if (stage.MonsterCount > 0 && (monsterModel == null || monsterModel.Prefab == null)) {
						Debug.LogError($"[Battle] Stage {stageId} has no spawnable monster for ID {stage.MonsterId}.", this);
						return;
					}

					CurrentStage = stage;
					Debug.Log($"[Battle] Stage {stage.Id} started ({stage.StageTime} seconds, monster {stage.MonsterId} x{stage.MonsterCount}).", this);
					await UniTask.WhenAll(
						SpawnMonstersAsync(stage, monsterModel, cancellationToken),
						UniTask.Delay(TimeSpan.FromSeconds(stage.StageTime), cancellationToken: cancellationToken));
				}
			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
				// The battle object was destroyed while waiting for the next stage.
			}
		}

		private async UniTask SpawnMonstersAsync(StageModel stage, MonsterModel model, CancellationToken cancellationToken) {
			for (int i = 0; i < stage.MonsterCount; i++) {
				cancellationToken.ThrowIfCancellationRequested();
				MonsterObj monster = _monsterFactory.Create(model, _map);
				if (monster.IsAlive) {
					monster.Released += OnMonsterReleased;
					_aliveMonsters.Add(monster);
				}

				if (i + 1 < stage.MonsterCount) {
					await UniTask.Delay(MonsterSpawnInterval, cancellationToken: cancellationToken);
				}
			}
		}

		private void OnMonsterReleased(MonsterObj monster) {
			monster.Released -= OnMonsterReleased;
			_aliveMonsters.Remove(monster);
		}

		private void OnEnable() {
			_input.AddSelectPressedListener(_inputLogic.OnSelectPressed);
			_input.AddSelectReleasedListener(_inputLogic.OnSelectReleased);
			_input.AddMoveListener(_inputLogic.OnMoveCommand);
			_input.AddDrawArticleListener(OnTestDrawArticle);
			Debug.Log("[BattleInput] BattleManager registered input listeners.", this);
		}

		private void OnDisable() {
			_input.RemoveSelectPressedListener(_inputLogic.OnSelectPressed);
			_input.RemoveSelectReleasedListener(_inputLogic.OnSelectReleased);
			_input.RemoveMoveListener(_inputLogic.OnMoveCommand);
			_input.RemoveDrawArticleListener(OnTestDrawArticle);
			_inputLogic.CancelDrag();
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

		public bool TryDrawArticle(out ArticleObj article) {
			return ShopLogic.TryDrawArticle(_map, _articleFactory, out article);
		}

#region Test Code
		private void OnTestDrawArticle() {
			if (TryDrawArticle(out ArticleObj article)) {
				Debug.Log($"[BattleInput] Drew article {article.Model.Id}, tier {article.Tier}.", article);
			} else {
				Debug.LogWarning("[BattleInput] Article draw failed. Check Silver and loaded shop data.", this);
			}
		}
#endregion

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
	}
}
