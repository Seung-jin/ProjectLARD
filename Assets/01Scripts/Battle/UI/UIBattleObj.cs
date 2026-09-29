using LAD.Stat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LAD.Battle {
    [RequireComponent(typeof(BattleObj))]
    public abstract class UIBattleObj : MonoBehaviour {
		protected BattleObj BattleObj { get; private set; }
		protected StatController BoundStats { get; private set; }
		private BattleManager boundBattleManager;

        protected virtual void Awake() {
            BattleObj = GetComponent<BattleObj>();
            CacheViews();
        }

        protected virtual void OnEnable() {
            BattleObj ??= GetComponent<BattleObj>();
            CacheViews();
            BattleObj.Observer.AddListener(BattleObjEventType.Initialized, OnBattleObjInitialized);
			BattleObj.Observer.AddListener(BattleObjEventType.ResourceChanged, OnBattleObjResourceChanged);
			BindBattleManager();
			BindStats();
        }

        protected virtual void OnDisable() {
            if (BattleObj != null) {
                BattleObj.Observer.RemoveListener(BattleObjEventType.Initialized, OnBattleObjInitialized);
                BattleObj.Observer.RemoveListener(BattleObjEventType.ResourceChanged, OnBattleObjResourceChanged);
			}
			UnbindBattleManager();
			UnbindStats();
        }

        private void OnBattleObjInitialized(params object[] args) {
            BindStats();
            OnInitialized();
        }

        private void OnBattleObjResourceChanged(params object[] args) {
            if (args is { Length: >= 3 } && args[1] is ResourceType type && args[2] is float value)
                RefreshResource(type, value);
        }

        private void BindStats() {
            StatController stats = BattleObj != null ? BattleObj.StatController : null;
            if (ReferenceEquals(BoundStats, stats)) {
                RefreshAll();
                return;
            }
            UnbindStats();
            BoundStats = stats;
            if (BoundStats != null) BoundStats.OnStatChanged += OnStatChanged;
            RefreshAll();
        }

		private void UnbindStats() {
            if (BoundStats == null) return;
            BoundStats.OnStatChanged -= OnStatChanged;
            BoundStats = null;
		}

		private void BindBattleManager() {
			UnbindBattleManager();
			boundBattleManager = BattleManager.Current;
			boundBattleManager?.Observer.AddListener(BattleManagerEventType.GlobalStatChanged, OnGlobalStatChanged);
		}

		private void UnbindBattleManager() {
			boundBattleManager?.Observer.RemoveListener(BattleManagerEventType.GlobalStatChanged, OnGlobalStatChanged);
			boundBattleManager = null;
		}

		private void OnGlobalStatChanged(params object[] args) {
			RefreshAll();
		}

        protected void RefreshBar(GameObject root, Image fill, TMP_Text valueText,
                                  float currentValue, StatType maxStatType) {
			float maximumValue = BattleStatLogic.GetStat(BattleObj, maxStatType);
            bool visible = maximumValue > 0f;
            SetVisible(root, fill, visible);
            if (fill != null) fill.fillAmount = visible ? Mathf.Clamp01(currentValue / maximumValue) : 0f;
            if (valueText != null) valueText.text = $"{currentValue:0.##}/{maximumValue:0.##}";
        }

        protected static void SetVisible(GameObject root, Component view, bool visible) {
            GameObject target = root != null ? root : view != null ? view.gameObject : null;
            if (target != null && target.activeSelf != visible) target.SetActive(visible);
        }

        protected virtual void CacheViews() { }
        protected virtual void OnInitialized() { }
        protected abstract void RefreshResource(ResourceType type, float value);
        protected abstract void OnStatChanged(StatType type, float value);
        public abstract void RefreshAll();
    }
}
