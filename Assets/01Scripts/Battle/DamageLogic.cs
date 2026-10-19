using LARD.Article;
using LARD.Skill;
using LARD.Buff;
using LARD.Upgrade;
using UnityEngine;

namespace LARD.Battle {
    /// <summary>
    /// 스킬 피해에 강화, 버프, 방어력과 받는 피해 증가 효과를 반영해 최종 피해를 적용한다.
    /// </summary>
    public static class DamageLogic {
        public static void ApplyDamage(ArticleObj attacker, float skillDamage, IAttackTarget target) {
            ArticleModel model = attacker.Model;
            float baseDamage = Mathf.Max(0f, skillDamage);
            int upgradeLevel = 0;
            float upgradePercent = GetUpgradePercent(model, out upgradeLevel);
            float skillBuffPercent = attacker.BuffController.GetTotalValue(BuffType.SkillDamageIncrease);
            float defense = target.DefensePercent;
            float damageTakenIncrease = target is LARD.Monster.MonsterObj affectedMonster
                ? affectedMonster.EffectController.DamageTakenIncreasePercent : 0f;
            float mitigatedDamage = Mathf.Floor(baseDamage * (1f + upgradePercent / 100f) *
                                                (1f + skillBuffPercent / 100f) *
                                                (1f - Mathf.Clamp(defense, 0f, 100f) / 100f));
            float damage = mitigatedDamage * (1f + damageTakenIncrease / 100f);

            // TakeDamage can release a defeated monster to its pool, so snapshot its identity first.
            if (target is LARD.Monster.MonsterObj monster) {
                int monsterObjectId = monster.Id;
                int monsterId = monster.Model != null ? monster.Model.Id : 0;
                float healthBefore = monster.StatController.CurrentHealth;
                target.TakeDamage(damage);
                BattleLog.Add(new BattleLogEntry(
                    Time.time, attacker.Id, model.Id, model.Race.ToString(),
                    upgradeLevel, upgradePercent, baseDamage, damage,
                    monsterObjectId, monsterId, defense, healthBefore,
                    monster.StatController.CurrentHealth));
                return;
            }

            target.TakeDamage(damage);
        }

        public static float CalculateDamage(ArticleModel attacker, float defensePercent) {
            float baseDamage = 0f;
            if (attacker.Skill?.Effects != null) {
                foreach (EffectData effect in attacker.Skill.Effects) {
                    if (effect == null || effect.Effect != SkillEffectType.Damage) continue;
                    baseDamage = Mathf.Max(0f, effect.Damage);
                    break;
                }
            }
            float upgradePercent = GetUpgradePercent(attacker, out _);
            float upgradedDamage = baseDamage * (1f + upgradePercent / 100f);
            return Mathf.Floor(upgradedDamage * (1f - Mathf.Clamp(defensePercent, 0f, 100f) / 100f));
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
