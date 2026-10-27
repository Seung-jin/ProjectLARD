namespace LARD.Skill {
    public enum EffectTargetType {
        NearestEnemy,
        DamagedEnemy,
        EnteredEnemy,
        AreaEnemy,
        NearbyAlly,
    }

    public enum SkillEffectType {
        Damage,
        DamageTakenIncrease,
        MoveSpeedDecrease,
        DefenseDecrease,
        SkillDamageIncrease,
        AttackSpeedIncrease,
    }

    public enum EffectApplicationTiming {
        Immediate,
        OnDamageDealt,
        OnEnter,
        OnTick,
    }

}
