using LAD.Accessory;
using LAD.Article;
using LAD.Player;
using LAD.Stat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LAD.Battle {
    /// <summary>기물의 성급, 마나, 장착 악세사리 UI를 표시합니다.</summary>
    public sealed class UIArticleBattleObj : UIBattleObj {
        [Header("Mana")]
        [SerializeField] private GameObject manaRoot;
        [SerializeField] private Image manaFill;
        [SerializeField] private TMP_Text manaText;

        [Header("Grade")]
        [SerializeField] private GameObject gradeRoot;
        [SerializeField] private TMP_Text gradeText;

        [Header("Accessory")]
        [SerializeField] private GameObject accessoryRoot;
        [SerializeField] private SlotEquippedAccessory[] accessorySlots;

        private PlayerAccessoryCollection accessoryCollection;

        protected override void CacheViews() {
            manaText ??= manaRoot != null ? manaRoot.GetComponentInChildren<TMP_Text>(true) : null;
            if (accessorySlots == null || accessorySlots.Length == 0)
                accessorySlots = GetComponentsInChildren<SlotEquippedAccessory>(true);
            if (accessoryRoot == null && accessorySlots is { Length: > 0 })
                accessoryRoot = accessorySlots[0].transform.parent.gameObject;
        }

        protected override void OnEnable() {
            base.OnEnable();
            BindAccessories();
        }

        protected override void OnDisable() {
            UnbindAccessories();
            base.OnDisable();
        }

        protected override void OnInitialized() {
            RefreshAccessories();
        }

        protected override void RefreshResource(ResourceType type, float value) {
            if (type == ResourceType.Mana) RefreshMana(value);
        }

        protected override void OnStatChanged(StatType type, float value) {
            if (type == StatType.MaxMana) RefreshMana(BoundStats.GetResource(ResourceType.Mana));
        }

        public override void RefreshAll() {
            if (BoundStats == null) {
                SetVisible(manaRoot, manaFill, false);
                SetVisible(gradeRoot, gradeText, false);
                SetVisible(accessoryRoot, null, false);
                return;
            }
            RefreshMana(BoundStats.GetResource(ResourceType.Mana));
            RefreshGrade();
            RefreshAccessories();
        }

        public void RefreshAccessories() {
            accessoryCollection ??= PlayerDataSet.PlayerAccessories;
            ArticleObj articleObj = BattleObj as ArticleObj;
            bool visible = articleObj?.PlayerArticle != null;
            SetVisible(accessoryRoot, null, visible);
            if (!visible || accessorySlots == null) return;

            foreach (SlotEquippedAccessory slot in accessorySlots) {
                LAD.Accessory.Accessory equipped = null;
                accessoryCollection?.TryGetEquipped(articleObj.PlayerArticle.InstanceId, slot.Parts, out equipped);
                slot.SetAccessory(equipped);
            }
        }

        private void RefreshMana(float mana) {
            RefreshBar(manaRoot, manaFill, manaText, mana, StatType.MaxMana);
        }

        private void RefreshGrade() {
            int grade = BattleObj != null ? BattleObj.Grade : 0;
            SetVisible(gradeRoot, gradeText, grade > 0);
            if (gradeText != null) gradeText.text = $"Grade {grade}";
        }

        private void BindAccessories() {
            UnbindAccessories();
            accessoryCollection = PlayerDataSet.PlayerAccessories;
            accessoryCollection?.Observer.AddListener(PlayerAccessoryEventType.Changed, OnAccessoriesChanged);
            RefreshAccessories();
        }

        private void UnbindAccessories() {
            accessoryCollection?.Observer.RemoveListener(PlayerAccessoryEventType.Changed, OnAccessoriesChanged);
            accessoryCollection = null;
        }

        private void OnAccessoriesChanged(params object[] args) {
            RefreshAccessories();
        }
    }
}
