using LARD.Article;
using LARD.Skill;
using LARD.Upgrade;
using UnityEngine;

namespace LARD.Battle {
    public static class DamageLogic {
        public static void ApplyDamage(ArticleObj attacker, float attackPowerRatio, IAttackTarget target) {
            ArticleModel model = attacker.Model;
            float baseDamage = CalculateBaseDamage(model, attackPowerRatio);
            int upgradeLevel = 0;
            float upgradePercent = GetUpgradePercent(model, out upgradeLevel);
            float defense = target.DefensePercent;
            float damage = Mathf.Floor(baseDamage * (1f + upgradePercent / 100f) *
                                      (1f - Mathf.Clamp(defense, 0f, 100f) / 100f));

            // TakeDamage can release a defeated monster to its pool, so snapshot its identity first.
            if (target is LARD.Monster.MonsterObj monster) {
                int monsterObjectId = monster.Id;
                int monsterId = monster.Model != null ? monster.Model.Id : 0;
                float healthBefore = monster.StatController.CurrentHealth;
                target.TakeDamage(damage);
                BattleLog.Add(new BattleLogEntry(
                    Time.time, attacker.Id, model.Id, model.AttackPower, model.Race.ToString(),
                    upgradeLevel, upgradePercent, baseDamage, damage,
                    monsterObjectId, monsterId, defense, healthBefore,
                    monster.StatController.CurrentHealth));
                return;
            }

            target.TakeDamage(damage);
        }

        public static float CalculateDamage(ArticleModel attacker, float attackPowerRatio, float defensePercent) {
            float baseDamage = CalculateBaseDamage(attacker, attackPowerRatio);
            float upgradePercent = GetUpgradePercent(attacker, out _);
            float upgradedDamage = baseDamage * (1f + upgradePercent / 100f);
            return Mathf.Floor(upgradedDamage * (1f - Mathf.Clamp(defensePercent, 0f, 100f) / 100f));
        }

        private static float CalculateBaseDamage(ArticleModel attacker, float attackPowerRatio) {
            return Mathf.Max(0f, attacker.AttackPower) * Mathf.Max(0f, attackPowerRatio);
        }

        private static float GetUpgradePercent(ArticleModel attacker, out int upgradeLevel) {
            upgradeLevel = 0;
            float upgradePercent = 0f;
            PlayerUpgrade playerUpgrade = UpgradeDataSet.GetPlayerUpgrade(attacker.Race);
            if (playerUpgrade != null) {
                upgradeLevel = playerUpgrade.Level;
                UpgradeModel upgradeModel = UpgradeDataSet.GetModel(attacker.Race, playerUpgrade.Level);
                if (upgradeModel != null) {
                    upgradePercent = Mathf.Max(0f, upgradeModel.AttackPowerIncreasePercent);
                }
            }

            return upgradePercent;
        }
    }
}
