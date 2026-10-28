using LSJ.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LARD.Battle {
	public class UIBattleResult : UIBase {
		[SerializeField] private TextMeshProUGUI _resultText;
		[SerializeField] private Button _restartBtn;

		public override void Init(UIType type) {
			base.Init(type);
			_restartBtn.onClick.AddListener(OnClickRestartBtn);
		}

		public override void Show(params object[] args) {
			base.Show(args);
			bool isWin = (bool)args[0];
			_resultText.text = isWin ? "You Win!" : "You Lose!";
		}

		public static object[] MakeParam(bool isWin) {
			return new object[] { isWin, };
		}

		private void OnClickRestartBtn() {
			bool result = BattleManager.Instance.TryRestartBattle();
			if (result) {
				UIManager.Instance.Hide(uiType);
			}
		}
	}
}