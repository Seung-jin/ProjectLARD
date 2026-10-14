using LSJ.UI;
using LARD.Article;
using LARD.Player;
using LARD.Stat;
using TMPro;
using LARD.Stage;
using LARD.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace LARD.Battle {
	public class UIBattleMain : UIBase {
		[SerializeField] private TextMeshProUGUI _stageText;
		[SerializeField] private TextMeshProUGUI _timeText;
		[SerializeField] private TextMeshProUGUI _silverText;
		[SerializeField] private TextMeshProUGUI _goldText;
		[SerializeField] private Button _summonBtn;
		[SerializeField] private Button _startBattleBtn;
		[SerializeField] private UIArticleInfo _articleInfo;
		[SerializeField] private SlotRaceUpgrade _yozUpgradeSlot;
		[SerializeField] private SlotRaceUpgrade _sylinUpgradeSlot;
		[SerializeField] private SlotRaceUpgrade _shushireUpgradeSlot;

		public override void Init(UIType type) {
			base.Init(type);
			_summonBtn.onClick.AddListener(OnClickSummonBtn);
			_startBattleBtn.onClick.AddListener(OnClickStartBattleBtn);
			_articleInfo.Init(UIType.ArticleInfo);
			_yozUpgradeSlot.Init(ArticleRace.Beastkin);
			_sylinUpgradeSlot.Init(ArticleRace.Mage);
			_shushireUpgradeSlot.Init(ArticleRace.Warrior);
		}

		public override void Show(params object[] args) {
			base.Show(args);
			UnbindBattleManager();
			UnbindResource();
			if (PlayerDataSet.Resource != null) {
				PlayerDataSet.Resource.BalanceChanged += OnBalanceChanged;
				UpdateBalances();
			}
			if (BattleManager.Instance == null) {
				_startBattleBtn.gameObject.SetActive(false);
				Debug.LogWarning("[UIBattleMain] BattleManager was not found.", this);
				return;
			}

			BattleManager.Instance.StageChanged += UpdateStage;
			BattleManager.Instance.StageTimeChanged += UpdateTime;
			BattleManager.Instance.SelectionChanged += UpdateArticleInfo;
			UpdateStage(BattleManager.Instance.CurrentStage);
			_startBattleBtn.gameObject.SetActive(!BattleManager.Instance.HasBattleStarted);
			_startBattleBtn.interactable = true;
			UpdateTime(BattleManager.Instance.RemainingStageSeconds);
			UpdateArticleInfo(BattleManager.Instance.SelectedArticle);
		}

		public override void Hide() {
			UnbindBattleManager();
			UnbindResource();
			_articleInfo.Hide();
			base.Hide();
		}

		private void OnDisable() {
			UnbindBattleManager();
			UnbindResource();
		}

		private void UnbindResource() {
			if (PlayerDataSet.Resource != null) {
				PlayerDataSet.Resource.BalanceChanged -= OnBalanceChanged;
			}
		}

		private void OnBalanceChanged(ResourceType type, long amount) {
			switch (type) {
				case ResourceType.Silver:
					_silverText.text = $"SILVER {amount:N0}";
					break;
				case ResourceType.Gold:
					_goldText.text = $"GOLD {amount:N0}";
					break;
			}
		}

		private void UpdateBalances() {
			OnBalanceChanged(ResourceType.Silver, PlayerDataSet.Resource.Silver);
			OnBalanceChanged(ResourceType.Gold, PlayerDataSet.Resource.Gold);
		}

		private void UnbindBattleManager() {
			if (BattleManager.Instance == null) {
				return;
			}

			BattleManager.Instance.StageChanged -= UpdateStage;
			BattleManager.Instance.StageTimeChanged -= UpdateTime;
			BattleManager.Instance.SelectionChanged -= UpdateArticleInfo;
		}

		private void UpdateArticleInfo(ArticleObj article) {
			if (article == null) {
				_articleInfo.Hide();
			} else {
				_articleInfo.Show(UIArticleInfo.MakeParam(article));
			}
		}

		private void UpdateStage(StageModel stage) {
			_stageText.text = stage == null ? "-" : $"Stage {stage.Id}";
			if (stage != null) {
				_startBattleBtn.gameObject.SetActive(false);
			}
		}

		private void UpdateTime(int remainingSeconds) {
			_timeText.text = $"{remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
		}

		private void OnClickSummonBtn() {
			if (BattleManager.Instance == null) {
				Debug.LogWarning("[UIBattleMain] BattleManager was not found.", this);
				return;
			}

			ShopLogic.TryPickAndSummonArticle();
		}

		private void OnClickStartBattleBtn() {
			if (BattleManager.Instance != null && BattleManager.Instance.TryStartBattle()) {
				_startBattleBtn.interactable = false;
			}
		}
	}
}
