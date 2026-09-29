namespace LAD.Effects {
	/// <summary>스킬, 각인, 악세사리, 시너지에서 공유하는 효과 종류입니다.</summary>
	/// <remarks>애셋에 정수로 저장되므로 기존 값을 변경하거나 재사용하지 않습니다.
	/// 효과의 대상, 발동 시점과 실행 방식은 각 시스템에서 결정합니다.</remarks>
	public enum EffectType {
		Damage = 0,
		ArmorReduction = 1,
		DamageTakenIncrease = 2,
		/// <summary>스킬 추가 실행 횟수. 1이면 기본 실행을 포함해 총 2회 실행합니다.</summary>
		SkillRepeatCount = 3,
		DamageIncrease = 4,
		GoldIncrease = 5,
		ExperienceIncrease = 6,
		CriticalChanceIncrease = 7,
		ManaRegenIncrease = 8,
		PoisonDamage = 9,
		LightningStrike = 10,
		ConditionalDamageIncrease = 11,
		MoveSpeedReduction = 12,
		ManaFlow = 13,
		DamageReductionPoints = 14,
		Stun = 15,
		Root = 16,
		GoldRewardIncrease = 17,
		ExperienceIncreaseFlat = 18,
		RandomTierArticleReward = 19,
		SynergyCountIncrease = 20,
		MaxInterestIncrease = 21,
		DeployableArticleCountIncrease = 22,
		RerollTicket = 23,
		UpgradeAllArticlesTier = 24,
	}
}
