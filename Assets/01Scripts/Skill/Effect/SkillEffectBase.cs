using LAD.Battle;

namespace LAD.Skill.Effect {
    /// <summary>EffectData 한 종류의 실제 적용 로직을 담당합니다.</summary>
    public abstract class SkillEffectBase {
        public abstract bool Activate(EffectData data, BattleObj source, BattleObj target);
    }
}
