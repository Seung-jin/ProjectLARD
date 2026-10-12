using LARD.Article;
using LARD.Player;
using LARD.Stat;
using LARD.Upgrade;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LARD.Battle {
    public sealed class SlotRaceUpgrade : MonoBehaviour {
        [SerializeField] private TextMeshProUGUI _raceText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Button _upgradeButton;

        private ArticleRace _race;
        private PlayerUpgrade _upgrade;
        private PlayerResource _resource;
        private bool _initialized;

        public void Init(ArticleRace race) {
            if (_initialized) {
                Unbind();
                _upgradeButton.onClick.RemoveListener(OnClickUpgrade);
            }

            _race = race;
            _initialized = true;
            _upgradeButton.onClick.AddListener(OnClickUpgrade);
            Bind();
            Refresh();
        }

        private void OnEnable() {
            if (!_initialized) {
                return;
            }

            Bind();
            Refresh();
        }

        private void OnDisable() => Unbind();

        private void OnDestroy() {
            Unbind();
            _upgradeButton.onClick.RemoveListener(OnClickUpgrade);
        }

        private void Bind() {
            Unbind();
            _upgrade = UpgradeDataSet.GetPlayerUpgrade(_race);
            _resource = PlayerDataSet.Resource;
            if (_upgrade != null) {
                _upgrade.LevelChanged += OnLevelChanged;
            }
            if (_resource != null) {
                _resource.BalanceChanged += OnBalanceChanged;
            }
        }

        private void Unbind() {
            if (_upgrade != null) {
                _upgrade.LevelChanged -= OnLevelChanged;
                _upgrade = null;
            }
            if (_resource != null) {
                _resource.BalanceChanged -= OnBalanceChanged;
                _resource = null;
            }
        }

        private void OnLevelChanged(int level) => Refresh();

        private void OnBalanceChanged(ResourceType type, long amount) {
            if (type == ResourceType.Gold) {
                Refresh();
            }
        }

        private void Refresh() {
            _raceText.text = _race.ToString().ToUpperInvariant();
            _levelText.text = $"LV {_upgrade?.Level ?? 0}";

            UpgradeModel current = _upgrade == null ? null : UpgradeDataSet.GetModel(_race, _upgrade.Level);
            UpgradeModel next = _upgrade == null || _upgrade.Level == int.MaxValue
                ? null : UpgradeDataSet.GetModel(_race, _upgrade.Level + 1);
            _upgradeButton.interactable = current != null && next != null && current.NextLevelCost >= 0 &&
                                          _resource != null && _resource.Gold >= current.NextLevelCost;
        }

        private void OnClickUpgrade() {
            UpgradeLogic.TryUpgrade(_race);
            Refresh();
        }
    }
}