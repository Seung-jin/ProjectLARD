using System;
using UnityEngine;

namespace LARD.Skill {
    [Serializable]
    public sealed class EffectData {
        public EffectTargetType Target = EffectTargetType.NearestEnemy;
        public SkillEffectType Effect = SkillEffectType.Damage;
        public EffectApplicationTiming Timing = EffectApplicationTiming.Immediate;
        [Min(0f)] public float Damage = 10f;
        [Min(1)] public int HitCount = 1;
    }
}
