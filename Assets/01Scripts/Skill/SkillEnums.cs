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
    }

    public enum EffectApplicationTiming {
        Immediate,
        OnDamageDealt,
        OnEnter,
        OnTick,
    }

    public enum EffectStackPolicy {
    }

    public enum EffectDurationPolicy {
    }

    public enum EffectStackScope {
    }

}
