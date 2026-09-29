using LAD.Effects;
using LAD.Battle;
using UnityEngine;

namespace LAD.Skill {
    public sealed class BasicTargetEffectSkill : SkillBase {
        private readonly EffectType effectType;
        private readonly string valueKey;
        private readonly string durationKey;
        private readonly string stackKey;

        public BasicTargetEffectSkill(
            EffectType effectType,
            string valueKey,
            string durationKey,
            string stackKey) {
            this.effectType = effectType;
            this.valueKey = valueKey;
            this.durationKey = durationKey;
            this.stackKey = stackKey;
        }

        public override bool Activate() {
            if (!TryGetCurrentTarget(out IDamageable target) || target is not BattleObj battleTarget ||
                GradeData == null || !GradeData.TryGetValue(valueKey, out float value) ||
                !GradeData.TryGetValue(durationKey, out float duration)) return false;

            EffectData effect = EffectData.CreateRuntime(
                effectType, Mathf.Abs(value), Mathf.Max(0f, duration), stackKey);
            bool activated = ActivateEffect(effect, battleTarget);
            return activated;
        }
    }
}
