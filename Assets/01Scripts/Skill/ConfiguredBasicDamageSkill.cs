using LAD.Battle;
using LAD.Stat;

namespace LAD.Skill {
    /// <summary>데이터만으로 구성되는 단순 기본 공격/스킬용 구체 구현입니다.</summary>
    public sealed class ConfiguredBasicDamageSkill : BasicDamageSkill {
        public ConfiguredBasicDamageSkill(DamageArea area, StatType scalingStat, DamageType damageType,
                                          bool notifySkillHit = false)
            : base(area, scalingStat, damageType, notifySkillHit) { }
    }
}
