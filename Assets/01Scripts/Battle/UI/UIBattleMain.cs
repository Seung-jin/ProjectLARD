using LSJ.UI;
using LARD.Player;
using LARD.Stat;
using TMPro;
using LARD.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace LARD.Battle.UI {
	public class UIBattleMain : UIBase {
		[SerializeField] private TextMeshProUGUI _stageText;
		[SerializeField] private TextMeshProUGUI _timeText;
		[SerializeField] private TextMeshProUGUI _silverText;
		[SerializeField] private TextMeshProUGUI _goldText;
		[SerializeField] private Button _summonBtn;

		public override void Init(UIType type) {
			base.Init(type);
			_summonBtn.onClick.AddListener(OnClickSummonBtn);
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
				Debug.LogWarning("[UIBattleMain] BattleManager was not found.", this);
				return;
			}

			BattleManager.Instance.StageChanged += UpdateStage;
			BattleManager.Instance.StageTimeChanged += UpdateTime;
			UpdateStage(BattleManager.Instance.CurrentStage);
			UpdateTime(BattleManager.Instance.RemainingStageSeconds);
		}

		public override void Hide() {
			UnbindBattleManager();
			UnbindResource();
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
		}

		private void UpdateStage(StageModel stage) {
			_stageText.text = stage == null ? "-" : $"Stage {stage.Id}";
		}

		private void UpdateTime(int remainingSeconds) {
			_timeText.text = $"{remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
		}

		private void OnClickSummonBtn() {
			if (BattleManager.Instance == null) {
				Debug.LogWarning("[UIBattleMain] BattleManager was not found.", this);
				return;
			}

			BattleManager.Instance.SummonArticle();
		}
	}
}
