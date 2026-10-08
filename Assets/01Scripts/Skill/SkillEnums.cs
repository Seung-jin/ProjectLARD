namespace LARD.Skill {
    public enum EffectTargetType {
        NearestEnemy,
        DamagedEnemy,
        EnteredEnemy,
        AreaEnemy,
    }

    public enum SkillEffectType {
        Damage,
        DamageTakenIncrease,
        MoveSpeedDecrease,
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
