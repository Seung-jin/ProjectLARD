using System;
using UnityEngine;

namespace LARD.Skill {
    [Serializable]
    public sealed class EffectData {
        public EffectTargetType Target = EffectTargetType.NearestEnemy;
        public SkillEffectType Effect = SkillEffectType.Damage;
        public EffectApplicationTiming Timing = EffectApplicationTiming.Immediate;
        [Min(1)] public int HitCount = 1;
        [Min(0)] public int MaxTargets;
        [Min(0f)] public float DurationSeconds;
        [Min(0f)] public float ValuePercent;
        [Min(0f)] public float Radius;
        [Range(0f, 360f)] public float ConeAngleDegrees;
    }
}
