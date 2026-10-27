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
		[SerializeField] private TextMeshProUGUI _saleCostText;
		[SerializeField] private Button _saleBtn;

		private ArticleObj _article;

		public override void Init(UIType type) {
			base.Init(type);

			_promotionBtn.onClick.AddListener(OnClickPromotionBtn);
			_saleBtn.onClick.AddListener(OnClickSaleBtn);
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
			BuildSale();
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
			BattleManager manager = BattleManager.Instance;
			if (manager == null || !manager.CanUpgradeSelectedArticles(out _)) {
				_promotionCostText.text = manager != null && manager.SelectedArticles.Count == 3
					? "INVALID COMBINATION" : "SELECT 3 ARTICLES";
				_promotionBtn.interactable = false;
				return;
			}

			_promotionCostText.text = "PROMOTE 3 ARTICLES";
			_promotionBtn.interactable = true;
		}

		private void OnClickPromotionBtn() {
			if (BattleManager.Instance != null) {
				BattleManager.Instance.TryUpgradeSelectedArticles();
			}
		}

		private void BuildSale() {
			BattleManager manager = BattleManager.Instance;
			if (manager == null || manager.SelectedArticle != _article ||
			    !ArticleLogic.TryGetSaleCost(_article, out int cost)) {
				_saleCostText.text = "NO SALE";
				_saleBtn.interactable = false;
				return;
			}

			_saleCostText.text = $"SELL FOR {cost:N0} SILVER";
			_saleBtn.interactable = PlayerDataSet.Resource != null;
		}

		private void OnClickSaleBtn() {
			BattleManager manager = BattleManager.Instance;
			if (manager != null && manager.SelectedArticle == _article) {
				manager.TrySellSelectedArticle();
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
