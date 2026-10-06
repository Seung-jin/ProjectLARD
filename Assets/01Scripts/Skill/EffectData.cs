using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace LARD.Skill {
    [Serializable]
    public sealed class EffectData {
        public EffectTargetType Target = EffectTargetType.NearestEnemy;
        public SkillEffectType Effect = SkillEffectType.Damage;
        public EffectApplicationTiming Timing = EffectApplicationTiming.Immediate;
        [FormerlySerializedAs("Damage")]
        [FormerlySerializedAs("AttackPowerPercent")]
        [Min(0f), Tooltip("Damage multiplier of the article's attack power (1 = 100%).")]
        public float AttackPowerRatio = 1f;
        [Min(1)] public int HitCount = 1;
    }
}
