using LAD.Battle;
using LAD.Stat;

namespace LAD.Skill {
    public sealed class BasicArticleSkill : BasicDamageSkill {
        public BasicArticleSkill(DamageArea area, StatType scalingStat, DamageType damageType)
            : base(area, scalingStat, damageType, true) { }
    }
}
