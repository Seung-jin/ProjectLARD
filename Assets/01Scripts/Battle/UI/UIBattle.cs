using LSJ.UI;
using LAD.Accessory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LAD.Battle {
	public sealed class UIBattle : UIBase {
		[SerializeField] private TextMeshProUGUI roundText;
		[SerializeField] private TextMeshProUGUI remainingUnitText;
		[SerializeField] private Button startButton;
		[SerializeField] private UIAccessoryInventory accessoryInventory;

		private BattleManager battleManager;

		public override void Init(UIType type) {
			base.Init(type);
			EnsureView();
			startButton?.onClick.AddListener(OnClickStart);
			accessoryInventory?.Initialize();
		}

		public override void Show(params object[] args) {
			base.Show(args);
			if (args is { Length: > 0 } && args[0] is BattleManager manager) {
				Bind(manager);
			}
			accessoryInventory?.Bind();
		}

		public override void Hide() {
			accessoryInventory?.Unbind();
			base.Hide();
		}

		private void Bind(BattleManager manager) {
			Unbind();
			battleManager = manager;
			if (battleManager == null) {
				return;
			}

			battleManager.Observer.AddListener(BattleManagerEventType.StateChanged, OnStateChanged);
			battleManager.Observer.AddListener(BattleManagerEventType.RoundChanged, OnRoundChanged);
			battleManager.Observer.AddListener(BattleManagerEventType.RemainingUnitCountChanged,
			                                   OnRemainingUnitCountChanged);

			SetRound(battleManager.CurrentRoundId);
			SetRemainingUnitCount(battleManager.RemainingUnitCount);
			SetState(battleManager.CurrentState);
		}

		private void OnClickStart() {
			battleManager?.StartCombat();
		}

		private void EnsureView() {
			if (roundText == null) {
				roundText = CreateText("RoundText", new Vector2(0.5f, 1f), new Vector2(-110f, -45f),
				                       new Vector2(200f, 50f), "Round 1");
			}

			if (remainingUnitText == null) {
				remainingUnitText = CreateText("RemainingUnitText", new Vector2(0.5f, 1f),
				                               new Vector2(110f, -45f), new Vector2(200f, 50f),
				                               "Remaining 0");
			}

			if (startButton == null) {
				GameObject buttonObject = new GameObject("StartButton", typeof(RectTransform), typeof(CanvasRenderer),
				                                             typeof(Image), typeof(Button));
				buttonObject.transform.SetParent(transform, false);
				RectTransform rect = (RectTransform)buttonObject.transform;
				rect.anchorMin = new Vector2(1f, 0f);
				rect.anchorMax = new Vector2(1f, 0f);
				rect.pivot = new Vector2(1f, 0f);
				rect.anchoredPosition = new Vector2(-40f, 40f);
				rect.sizeDelta = new Vector2(180f, 70f);
				buttonObject.GetComponent<Image>().color = new Color(0.16f, 0.55f, 0.95f, 0.95f);
				startButton = buttonObject.GetComponent<Button>();
				CreateText("Label", new Vector2(0.5f, 0.5f), Vector2.zero, rect.sizeDelta, "Battle Start",
				           buttonObject.transform);
			}
		}

		private TextMeshProUGUI CreateText(string objectName, Vector2 anchor, Vector2 position,
		                                         Vector2 size, string value, Transform parent = null) {
			GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
			                                       typeof(TextMeshProUGUI));
			textObject.transform.SetParent(parent != null ? parent : transform, false);
			RectTransform rect = (RectTransform)textObject.transform;
			rect.anchorMin = anchor;
			rect.anchorMax = anchor;
			rect.anchoredPosition = position;
			rect.sizeDelta = size;

			TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
			text.text = value;
			text.fontSize = 30f;
			text.alignment = TextAlignmentOptions.Center;
			text.color = Color.white;
			return text;
		}

		private void OnStateChanged(params object[] args) {
			if (args is { Length: > 0 } && args[0] is BattleStateType state) {
				SetState(state);
			}
		}

		private void OnRoundChanged(params object[] args) {
			if (args is { Length: > 0 } && args[0] is int roundId) {
				SetRound(roundId);
			}
		}

		private void OnRemainingUnitCountChanged(params object[] args) {
			if (args is { Length: > 0 } && args[0] is int count) {
				SetRemainingUnitCount(count);
			}
		}

		private void SetState(BattleStateType state) {
			if (startButton != null) {
				startButton.gameObject.SetActive(state == BattleStateType.Maintain);
			}
		}

		private void SetRound(int roundId) {
			if (roundText != null) {
				roundText.text = $"Round {roundId}";
			}
		}

		private void SetRemainingUnitCount(int count) {
			if (remainingUnitText != null) {
				remainingUnitText.text = $"Remaining {count}";
			}
		}

		private void Unbind() {
			if (battleManager == null) {
				return;
			}

			battleManager.Observer.RemoveListener(BattleManagerEventType.StateChanged, OnStateChanged);
			battleManager.Observer.RemoveListener(BattleManagerEventType.RoundChanged, OnRoundChanged);
			battleManager.Observer.RemoveListener(BattleManagerEventType.RemainingUnitCountChanged,
			                                      OnRemainingUnitCountChanged);
			battleManager = null;
		}

		private void OnDestroy() {
			startButton?.onClick.RemoveListener(OnClickStart);
			Unbind();
		}
	}
}
