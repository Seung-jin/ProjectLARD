using LAD.Player;
using LAD.Round;
using LAD.Stat;
using UnityEngine;

namespace LAD.Battle {
    /// <summary>
    /// 전투에서 사용하는 공통 계산과 적용 절차를 담당합니다.
    /// </summary>
    public static class BattleLogic {
		private const float BaseDamageMultiplier = 1f;
		private const float CriticalChancePerPoint = 0.0025f;
		private const float CriticalDamageIncreasePerPoint = 0.005f;
		/// <summary>
		/// 완료한 라운드에 설정된 골드 보상을 플레이어에게 지급합니다.
		/// </summary>
		/// <returns>보상 지급에 성공했으면 true입니다.</returns>
		public static bool GrantRoundClearReward(RoundModel roundModel) {
			if (roundModel == null) {
				Debug.LogError("보상을 지급할 RoundModel이 없습니다.");
				return false;
			}

			PlayerResource playerResource = PlayerDataSet.PlayerResource;
			if (playerResource == null) {
				Debug.LogError("PlayerResource가 초기화되지 않아 라운드 보상을 지급할 수 없습니다.");
				return false;
			}

			int goldReward = Mathf.Max(0, roundModel.ClearRewardGold + PlayerDataSet.Economy.GoldRewardBonus);
			playerResource.AddGold(goldReward);
			PlayerDataSet.PlayerShop?.AddExperience(PlayerDataSet.Economy.ExperienceRewardBonus);
			Debug.Log($"라운드 클리어 보상으로 골드 {goldReward}을 획득했습니다. 현재 골드: {playerResource.Gold}");
			return true;
		}

        /// <summary>
        /// 원본 대미지에 대상의 스탯 계산을 반영한 뒤 대상에게 적용합니다.
        /// </summary>
        /// <returns>대상에게 실제로 적용된 대미지입니다.</returns>
        public static float ApplyDamage(float damage, IDamageable target, DamageType damageType = DamageType.Physical) {
            return ApplyDamage(damage, null, target, damageType);
        }

        /// <summary>
        /// 공격 주체와 대상을 포함하여 대미지를 적용하고 양쪽의 전투 이벤트를 발송합니다.
        /// </summary>
        public static float ApplyDamage(float damage, BattleObj attacker, IDamageable target,
                                        DamageType damageType = DamageType.Physical, string attackKind = "기타 피해",
                                        AttackCategory category = AttackCategory.Other,
                                        float damageReductionIgnoreRatio = 0f) {
            if (target == null) {
                Debug.LogError("대미지를 적용할 대상이 없습니다.");
                return 0f;
            }

            if (target.StatController == null) {
                Debug.LogError("대상의 StatController가 없습니다.");
                return 0f;
            }

            if (target is BattleObj victim && (victim == null || victim.IsDead || !victim.gameObject.activeInHierarchy)) return 0f;
            if (attacker is LAD.Article.ArticleObj article &&
                (!article.IsOnBattlefield || (BattleManager.Current != null && !BattleManager.Current.IsRoundActive))) return 0f;

            attacker?.NotifyAttack(target);
            float rawDamage = damage;

            AttackParameters parameters = new AttackParameters(attacker, target, rawDamage, 0f,
                damageType, false, attackKind, category);
            attacker?.Observer.TriggerEvent(BattleObjEventType.BeforeDamage, parameters);
            damage *= Mathf.Max(0f, parameters.DamageMultiplier);
			damage *= 1f + Mathf.Max(0f, BattleStatLogic.GetStat(attacker, StatType.DamageIncrease));
            if (category == AttackCategory.BasicAttack) {
                damage *= 1f + Mathf.Max(0f, BattleStatLogic.GetStat(attacker, StatType.BasicAttackDamageIncrease));
            }
            else if (category == AttackCategory.Skill) {
                damage *= 1f + Mathf.Max(0f, BattleStatLogic.GetStat(attacker, StatType.SkillDamageIncrease));
            }
            bool isCritical = category != AttackCategory.Periodic && category != AttackCategory.Effect &&
                              TryApplyCriticalDamage(ref damage, attacker);
            float calculatedDamage = CalculateDamage(damage, target.StatController, damageType, damageReductionIgnoreRatio);
            BattleCombatLog.RecordDamage(attacker, target, attackKind, damageType, rawDamage, damage, calculatedDamage, isCritical);
            target.TakeDamage(calculatedDamage);
            parameters.Damage = calculatedDamage;
            parameters.IsCritical = isCritical;
            if (attacker != null) attacker.NotifyDamageDealt(parameters);
            else BattleManager.Current?.Observer.TriggerEvent(BattleManagerEventType.DamageDealt, parameters);

            string attackerName = attacker != null ? attacker.name : "공격자 정보 없음";
            string targetName = target is Object targetObject ? targetObject.name : target.GetType().Name;
            Object logContext = attacker != null ? attacker : target as Object;
            Debug.Log(
                $"{attackerName}이(가) {targetName}에게 {calculatedDamage:0.##}의 {damageType} " +
                $"{(isCritical ? "치명타 " : string.Empty)}대미지를 입혔습니다.",
                logContext);

            return calculatedDamage;
        }

		private static bool TryApplyCriticalDamage(ref float damage, BattleObj attacker) {
			if (attacker?.StatController == null) {
				return false;
			}

			float criticalChance = GetCriticalChance(attacker);
            if (criticalChance <= 0f ||
                (criticalChance < 1f && Random.value >= criticalChance)) {
                return false;
            }

            damage *= GetCriticalDamageMultiplier(attacker);
            return true;
        }

        public static float GetCriticalChance(BattleObj attacker) => Mathf.Clamp01(
            BattleStatLogic.GetStat(attacker, StatType.Critical) * CriticalChancePerPoint +
            BattleStatLogic.GetStat(attacker, StatType.CriticalChance));

        public static float GetCriticalDamageMultiplier(BattleObj attacker) => BaseDamageMultiplier +
            BattleStatLogic.GetStat(attacker, StatType.Critical) * CriticalDamageIncreasePerPoint +
            BattleStatLogic.GetStat(attacker, StatType.CriticalDamageIncrease);

        private static float CalculateDamage(float damage, StatController targetStats, DamageType damageType,
                                             float damageReductionIgnoreRatio = 0f) {
            damage = Mathf.Max(0f, damage);
            if (damageType == DamageType.True) {
                return damage;
            }

            float reductionRatio = Mathf.Clamp01(targetStats.GetStat(StatType.DamageReduction) / 100f);
            reductionRatio *= 1f - Mathf.Clamp01(damageReductionIgnoreRatio);
            return damage * (1f - reductionRatio) *
                   (1f + targetStats.GetStat(StatType.DamageTakenIncrease));
        }
    }
}
