using LSJ.UI;
using LARD.Battle;
using LARD.Player;
using LARD.Stat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LARD.Article {
	public class UIArticleInfo : UIBase {
		[SerializeField] private TextMeshProUGUI _promotionCostText;
		[SerializeField] private Button _promotionBtn;

		private ArticleObj _article;

		public override void Init(UIType type) {
			base.Init(type);

			_promotionBtn.onClick.AddListener(OnClickPromotionBtn);
		}

		public override void Show(params object[] args) {
			base.Show(args);

			_article = args[0] as ArticleObj;
			UnbindResource();
			if (PlayerDataSet.Resource != null) {
				PlayerDataSet.Resource.BalanceChanged += OnBalanceChanged;
			}

			BuildArticleInfo();
			BuildPromotion();
		}

		public override void Hide() {
			UnbindResource();
			_article = null;
			base.Hide();
		}

		private void OnDisable() {
			UnbindResource();
		}

		public static object[] MakeParam(ArticleObj article) {
			return new object[] { article };
		}

		private void BuildArticleInfo() {
		}

		private void BuildPromotion() {
			if (!ArticleLogic.TryGetUpgradeCost(_article, out int cost)) {
				_promotionCostText.text = "NO PROMOTION";
				_promotionBtn.interactable = false;
				return;
			}

			_promotionCostText.text = $"COST {cost:N0} SILVER";
			_promotionBtn.interactable = PlayerDataSet.Resource != null && PlayerDataSet.Resource.Silver >= cost;
		}

		private void OnClickPromotionBtn() {
			if (BattleManager.Instance != null) {
				BattleManager.Instance.TryUpgradeSelectedArticle();
			}
		}

		private void OnBalanceChanged(ResourceType type, long amount) {
			if (type == ResourceType.Silver) {
				BuildPromotion();
			}
		}

		private void UnbindResource() {
			if (PlayerDataSet.Resource != null) {
				PlayerDataSet.Resource.BalanceChanged -= OnBalanceChanged;
			}
		}
	}
}