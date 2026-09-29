using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LAD.Article;
using LAD.Engraving;
using LAD.Monster;
using LAD.Player;
using LAD.Round;
using LAD.Shop;
using LAD.Stat;
using LSJ.State;
using LSJ.UI;
using LSJ.Util;
using UnityEngine;
using BattleMap = LAD.Map.Map;

namespace LAD.Battle {
	public sealed class BattleManager : MonoBehaviour {
		private const float MonsterSpawnInterval = 0.5f;

		[SerializeField] private BattleMap map;
		[SerializeField] private Transform articlePoolRoot;
		[SerializeField] private Transform monsterPoolRoot;

		private ArticleFactory articleFactory;
		private MonsterFactory monsterFactory;
		private PlayerDataSet playerDataSet;
		private StateMachine<BattleStateType> stateMachine;
		private readonly List<MonsterObj> spawnedMonsters = new List<MonsterObj>();
		private bool isSpawningMonsters;
		private bool isStartingGoldGranted;
		private bool shouldRefreshShopOnMaintain;
		private float remainingRoundTime;

		public static BattleManager Current { get; private set; }
		public Observer<BattleManagerEventType> Observer { get; } = new Observer<BattleManagerEventType>();
		public StatController GlobalStatController { get; } = new StatController();
		public BattleStateType CurrentState => stateMachine.CurrentState;
		public int CurrentRoundId { get; private set; } = 1;
		public int RemainingUnitCount { get; private set; }
		public RoundModel CurrentRound { get; private set; }
		public bool IsRoundActive { get; private set; }

		private void Awake() {
			Current = this;
			GlobalStatController.OnStatChanged += OnGlobalStatChanged;
			EngravingLogic.Bind(this);
			articleFactory = new ArticleFactory(articlePoolRoot);
			ArticleLogic.Initialize(articleFactory, map);
			monsterFactory = new MonsterFactory(monsterPoolRoot);
			playerDataSet = new PlayerDataSet();

			stateMachine = new StateMachine<BattleStateType>();
			stateMachine.AddState(BattleStateType.Start, new BattleStartState(this));
			stateMachine.AddState(BattleStateType.Load, new BattleLoadState(this));
			stateMachine.AddState(BattleStateType.Maintain, new BattleMaintainState(this));
			stateMachine.AddState(BattleStateType.Combat, new BattleCombatState(this));
			stateMachine.AddState(BattleStateType.Win, new BattleWinState(this));
			stateMachine.AddState(BattleStateType.Defeat, new BattleDefeatState(this));
			stateMachine.AddState(BattleStateType.GameOver, new BattleGameOverState(this));
			stateMachine.AddState(BattleStateType.GameClear, new BattleGameClearState(this));
		}

		private void Start() {
			ChangeState(BattleStateType.Start);
		}

		private void Update() {
			stateMachine.Update();
		}

		private void FixedUpdate() {
			stateMachine.FixedUpdate();
		}

		public void ChangeState(BattleStateType state) {
			if (state != BattleStateType.Combat) EndRound();
			stateMachine.ChangeState(state);
			Observer.TriggerEvent(BattleManagerEventType.StateChanged, state);
		}

		private void EndRound() {
			if (!IsRoundActive) return;
			IsRoundActive = false;
			Observer.TriggerEvent(BattleManagerEventType.RoundEnded, CurrentRoundId);
		}

		public void StartCombat() {
			if (CurrentState == BattleStateType.Maintain) {
				ChangeState(BattleStateType.Combat);
			}
		}

		internal void StartBattle() {
			StartBattleAsync().Forget();
		}

		private async UniTask StartBattleAsync() {
			await playerDataSet.Load();
			ChangeState(BattleStateType.Load);
		}

		internal UniTask<bool> LoadBattle() {
			return LoadBattleAsync();
		}

		private async UniTask<bool> LoadBattleAsync() {
			GameManager gameManager = GameManager.Instance;
			if (gameManager == null) {
				Debug.LogError("GameManager를 찾을 수 없습니다.", this);
				return false;
			}

			await gameManager.WaitForInitialization();

			BattleConstModel battleConst = BattleDataSet.Const;
			PlayerResource playerResource = PlayerDataSet.PlayerResource;
			if (battleConst == null || playerResource == null) {
				Debug.LogError("전투 상수 또는 플레이어 자원 데이터가 없어 시작 골드를 지급할 수 없습니다.", this);
				return false;
			}

			if (map == null) {
				Debug.LogError("BattleManager에 Map이 지정되지 않았습니다.", this);
				return false;
			}

			UIManager uiManager = UIManager.Instance;
			if (uiManager == null) {
				Debug.LogError("전투 UI를 열 UIManager가 씬에 없습니다.", this);
				return false;
			}

			if (!isStartingGoldGranted) {
				playerResource.AddGold(battleConst.StartingGold);
				playerResource.SetHealth(battleConst.StartingHealth);
				isStartingGoldGranted = true;
			}

			uiManager.Show(UIType.Battle, this);
			uiManager.Show(UIType.Shop);

			ChangeState(BattleStateType.Maintain);
			return true;
		}

		internal void StartCurrentRound() {
			SpawnCurrentRoundAsync().Forget();
		}

		internal void EnterMaintainState() {
			if (!shouldRefreshShopOnMaintain) {
				return;
			}

			shouldRefreshShopOnMaintain = false;
			if (!ShopLogic.Refresh(out List<int> articleIds)) {
				Debug.LogError("전투 종료 후 상점 기물 목록을 갱신하지 못했습니다.", this);
				return;
			}

			UIManager uiManager = UIManager.Instance;
			UIShop uiShop = uiManager != null ? uiManager.GetUI<UIShop>(UIType.Shop) : null;
			if (uiShop == null) {
				Debug.LogError("갱신된 기물 목록을 적용할 상점 UI가 없습니다.", this);
				return;
			}

			uiShop.SetArticles(articleIds);
		}

		internal void UpdateCurrentRound(float deltaTime) {
			if (CurrentState != BattleStateType.Combat || CurrentRound == null) {
				return;
			}

			remainingRoundTime -= Mathf.Max(0f, deltaTime);
			if (remainingRoundTime <= 0f) {
				HandleRoundTimeout();
			}
		}

		internal void CompleteRoundResult(BattleStateType resultState) {
			CompleteRoundResultAsync(resultState).Forget();
		}

		private async UniTask SpawnCurrentRoundAsync() {
			CurrentRound = RoundDataSet.Get(CurrentRoundId);
			if (CurrentRound == null) {
				Debug.LogError($"현재 라운드 데이터를 찾을 수 없습니다. Round ID: {CurrentRoundId}", this);
				ChangeState(BattleStateType.GameClear);
				return;
			}
			IsRoundActive = true;
			Observer.TriggerEvent(BattleManagerEventType.RoundStarted, CurrentRoundId);

			remainingRoundTime = CurrentRound.TimeLimit;

			// 보스 라운드도 일반 몬스터 데이터를 함께 사용할 수 있습니다.
			// 보스 ID·수량은 데이터에 보관만 하며, 실제 보스 스폰은 별도 로직에서 추가합니다.
			if (CurrentRound.MonsterId <= 0 || CurrentRound.MonsterCount <= 0) {
				if (CurrentRound.IsBossRound) {
					CheckRoundComplete();
					return;
				}
				Debug.LogError($"라운드 {CurrentRoundId}의 일반 몬스터 데이터가 없습니다. ID: {CurrentRound.MonsterId}, 수량: {CurrentRound.MonsterCount}", this);
				ChangeState(BattleStateType.Maintain);
				return;
			}
			if (!MonsterDataSet.TryGet(CurrentRound.MonsterId, out _)) {
				Debug.LogError($"라운드 {CurrentRoundId}에 지정된 몬스터를 찾을 수 없습니다. Monster ID: {CurrentRound.MonsterId}", this);
				ChangeState(BattleStateType.Maintain);
				return;
			}

			SetRemainingUnitCount(CurrentRound.MonsterCount);
			isSpawningMonsters = true;
			for (int i = 0; i < CurrentRound.MonsterCount; i++) {
				if (CurrentState != BattleStateType.Combat) {
					isSpawningMonsters = false;
					return;
				}

				MonsterObj monster = monsterFactory.Spawn(
					CurrentRound.MonsterId,
					map.MonsterSpawnPosition,
					map.MonsterSpawnRotation);

				if (monster != null) {
					monster.SetMovePath(map.MonsterPath);
					monster.Observer.AddListener(BattleObjEventType.Died, OnMonsterDied);
					spawnedMonsters.Add(monster);
				} else {
					SetRemainingUnitCount(RemainingUnitCount - 1);
				}

				if (i < CurrentRound.MonsterCount - 1) {
					await UniTask.Delay(TimeSpan.FromSeconds(MonsterSpawnInterval));
				}
			}

			isSpawningMonsters = false;
			CheckRoundComplete();
		}

		private void OnMonsterDied(params object[] args) {
			if (args.Length == 0 || !(args[0] is MonsterObj monster)) {
				return;
			}

			monster.Observer.RemoveListener(BattleObjEventType.Died, OnMonsterDied);
			spawnedMonsters.Remove(monster);
			SetRemainingUnitCount(RemainingUnitCount - 1);
			CheckRoundComplete();
		}

		private void CheckRoundComplete() {
			if (CurrentState != BattleStateType.Combat || isSpawningMonsters || spawnedMonsters.Count > 0) {
				return;
			}

			BattleLogic.GrantRoundClearReward(CurrentRound);
			if (CurrentRound.ShowEngravingChoice) {
				Observer.TriggerEvent(BattleManagerEventType.EngravingChoiceAvailable, CurrentRoundId);
			}

			int nextRoundId = CurrentRoundId + 1;
			if (RoundDataSet.TryGet(nextRoundId, out _)) {
				ChangeState(BattleStateType.Win);
				return;
			}

			ChangeState(BattleStateType.GameClear);
		}

		private void HandleRoundTimeout() {
			if (CurrentState != BattleStateType.Combat) {
				return;
			}

			isSpawningMonsters = false;
			CleanupCurrentRoundMonsters();

			PlayerResource playerResource = PlayerDataSet.PlayerResource;
			BattleConstModel battleConst = BattleDataSet.Const;
			if (playerResource == null || battleConst == null) {
				Debug.LogError("시간 초과 피해를 적용할 데이터가 없습니다.", this);
				ChangeState(BattleStateType.GameOver);
				return;
			}

			playerResource.AddHealth(-battleConst.TimeoutHealthDamage);
			ChangeState(BattleStateType.Defeat);
		}

		private async UniTask CompleteRoundResultAsync(BattleStateType resultState) {
			// 승패 상태가 실제로 반영된 뒤 임시 다음 단계로 전환합니다.
			await UniTask.Yield();
			if (CurrentState != resultState) {
				return;
			}

			PlayerResource playerResource = PlayerDataSet.PlayerResource;
			if (resultState == BattleStateType.Defeat && (playerResource == null || playerResource.Health <= 0)) {
				ChangeState(BattleStateType.GameOver);
				return;
			}

			int nextRoundId = CurrentRoundId + 1;
			if (!RoundDataSet.TryGet(nextRoundId, out _)) {
				ChangeState(BattleStateType.GameClear);
				return;
			}

			CurrentRoundId = nextRoundId;
			CurrentRound = null;
			remainingRoundTime = 0f;
			Observer.TriggerEvent(BattleManagerEventType.RoundChanged, CurrentRoundId);
			shouldRefreshShopOnMaintain = true;
			ChangeState(BattleStateType.Maintain);
		}

		private void CleanupCurrentRoundMonsters() {
			for (int i = spawnedMonsters.Count - 1; i >= 0; i--) {
				MonsterObj monster = spawnedMonsters[i];
				if (monster == null) {
					continue;
				}

				monster.Observer.RemoveListener(BattleObjEventType.Died, OnMonsterDied);
				monster.Release();
			}

			spawnedMonsters.Clear();
			SetRemainingUnitCount(0);
		}

		private void SetRemainingUnitCount(int count) {
			RemainingUnitCount = Mathf.Max(0, count);
			Observer.TriggerEvent(BattleManagerEventType.RemainingUnitCountChanged, RemainingUnitCount);
		}

		private void OnGlobalStatChanged(StatType type, float value) {
			Observer.TriggerEvent(BattleManagerEventType.GlobalStatChanged, type, value);
		}

		private void OnDestroy() {
			EndRound();
			EngravingLogic.Unbind(this);
			GlobalStatController.OnStatChanged -= OnGlobalStatChanged;
			if (Current == this) {
				Current = null;
			}
			for (int i = 0; i < spawnedMonsters.Count; i++) {
				if (spawnedMonsters[i] != null) {
					spawnedMonsters[i].Observer.RemoveListener(BattleObjEventType.Died, OnMonsterDied);
				}
			}

			spawnedMonsters.Clear();
			Observer.ClearAll();
			ArticleLogic.Clear();
			articleFactory?.Clear();
			monsterFactory?.Clear();
		}
	}
}
