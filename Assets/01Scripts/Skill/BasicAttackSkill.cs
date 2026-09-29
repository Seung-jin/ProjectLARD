using LAD.Battle;
using LAD.Stat;

namespace LAD.Skill {
    public sealed class BasicAttackSkill : BasicDamageSkill {
        public BasicAttackSkill(DamageArea area, StatType scalingStat, DamageType damageType)
            : base(area, scalingStat, damageType, false) { }
    }
}
