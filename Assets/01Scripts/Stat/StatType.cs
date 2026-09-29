namespace LAD.Stat {
    public enum StatType {
        // 명시적인 번호는 기존 ScriptableObject의 직렬화 값을 보존하기 위한 것입니다.
        MaxHealth = 0,
        MoveSpeed = 1,
        /// <summary>100일 때 초당 1회 공격하는 신속 수치입니다.</summary>
        Swiftness = 2,
        Strength = 3,
        Intelligence = 4,
        /// <summary>받는 피해를 백분율로 감소시킵니다. 25면 받는 피해가 25% 감소합니다.</summary>
        DamageReduction = 5,
        Range = 7,
        MaxMana = 8,
        ManaRegen = 9,
        /// <summary>0~1 범위의 치명타 발생 확률입니다.</summary>
        CriticalChance = 10,
        /// <summary>기본 피해 1.0에 더해지는 치명타 피해 증가량입니다. 0.5면 최종 1.5배입니다.</summary>
        CriticalDamageIncrease = 11,
        Dexterity = 12,
        Specialization = 13,
        /// <summary>100일 때 치명타 확률 25%, 치명타 추가 피해 50%를 제공하는 치명 수치입니다.</summary>
        Critical = 14,
        /// <summary>받는 피해 증가 비율입니다. 0.1이면 받는 피해가 10% 증가합니다.</summary>
        DamageTakenIncrease = 16,
		/// <summary>가하는 모든 피해에 추가로 곱해지는 비율입니다. 0.1이면 피해가 10% 증가합니다.</summary>
        DamageIncrease = 15,
		/// <summary>스킬 기본 실행 횟수에 더해지는 전역 실행 횟수입니다.</summary>
		SkillActivationCount = 17,
        /// <summary>기본 공격 피해에만 추가로 곱해지는 비율입니다.</summary>
        BasicAttackDamageIncrease = 18,
        SkillDamageIncrease = 19,
    }
}
