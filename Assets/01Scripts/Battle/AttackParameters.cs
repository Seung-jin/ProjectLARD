namespace LAD.Battle {
    public enum AttackCategory { Other, BasicAttack, Skill, Periodic, Effect }

    /// <summary>피해 계산 전/적중 후에 동일한 인스턴스로 전달하는 공격 정보입니다.</summary>
    public sealed class AttackParameters {
        public BattleObj Attacker { get; }
        public IDamageable Target { get; }
        public float RawDamage { get; }
        public float Damage { get; internal set; }
        public DamageType DamageType { get; }
        public bool IsCritical { get; internal set; }
        public string AttackKind { get; }
        public AttackCategory Category { get; }
        public long AttackId { get; }
        public bool IsDirectDamage => Category == AttackCategory.BasicAttack || Category == AttackCategory.Skill;
        public float DamageMultiplier { get; set; } = 1f;
        public int TargetLifeVersion { get; }
        public bool IsTargetAlive => Target is BattleObj obj && obj != null && !obj.IsDead &&
            obj.gameObject.activeInHierarchy && obj.LifeVersion == TargetLifeVersion;

        public AttackParameters(BattleObj attacker, IDamageable target, float rawDamage, float damage,
                                DamageType damageType, bool isCritical, string attackKind,
                                AttackCategory category = AttackCategory.Other) {
            Attacker = attacker; Target = target; RawDamage = rawDamage; Damage = damage;
            DamageType = damageType; IsCritical = isCritical; AttackKind = attackKind;
            Category = category;
            AttackId = attacker != null ? attacker.CurrentAttackId : 0;
            TargetLifeVersion = target is BattleObj obj ? obj.LifeVersion : 0;
        }
    }
}
