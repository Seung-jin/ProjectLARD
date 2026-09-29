using LAD.Stat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LAD.Battle {
    /// <summary>몬스터의 체력 UI만 표시합니다.</summary>
    public sealed class UIMonsterBattleObj : UIBattleObj {
        [Header("Health")]
        [SerializeField] private GameObject healthRoot;
        [SerializeField] private Image healthFill;
        [SerializeField] private TMP_Text healthText;

        protected override void CacheViews() {
            healthText ??= healthRoot != null ? healthRoot.GetComponentInChildren<TMP_Text>(true) : null;
        }

        protected override void RefreshResource(ResourceType type, float value) {
            if (type == ResourceType.Health) RefreshHealth(value);
        }

        protected override void OnStatChanged(StatType type, float value) {
            if (type == StatType.MaxHealth) RefreshHealth(BoundStats.GetResource(ResourceType.Health));
        }

        public override void RefreshAll() {
            if (BoundStats == null) {
                SetVisible(healthRoot, healthFill, false);
                return;
            }
            RefreshHealth(BoundStats.GetResource(ResourceType.Health));
        }

        private void RefreshHealth(float health) {
            RefreshBar(healthRoot, healthFill, healthText, health, StatType.MaxHealth);
        }
    }
}
