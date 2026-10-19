using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LARD.Skill {
    /// <summary>
    /// 스킬 하나에 포함되는 개별 이펙트의 대상, 실행 시점, 효과 값과 범위를 정의한다.
    /// </summary>
    [Serializable]
    public sealed class EffectData {
        public EffectTargetType Target = EffectTargetType.NearestEnemy;
        public SkillEffectType Effect = SkillEffectType.Damage;
        public EffectApplicationTiming Timing = EffectApplicationTiming.Immediate;
        [ShowIf(nameof(IsDamage)), Min(0f),
         Tooltip("Fixed damage per hit before upgrades, buffs, and defense.")]
        public float Damage;
        [ShowIf(nameof(IsDamage)), Min(1)] public int HitCount = 1;
        [ShowIf(nameof(TargetsNearbyAllies)), Min(0)] public int MaxTargets;
        [ShowIf(nameof(IsStatusEffect)), Min(0f)] public float DurationSeconds;
        [ShowIf(nameof(IsStatusEffect)), Min(0f)] public float ValuePercent;
        [ShowIf(nameof(UsesRadius)), Min(0f)] public float Radius;
        [ShowIf(nameof(UsesConeAngle)), Range(0f, 360f)] public float ConeAngleDegrees;

        private bool IsDamage => Effect == SkillEffectType.Damage;
        private bool IsStatusEffect => !IsDamage;
        private bool TargetsNearbyAllies => Target == EffectTargetType.NearbyAlly &&
                                             Timing == EffectApplicationTiming.OnDamageDealt;
        private bool UsesRadius => TargetsNearbyAllies ||
                                   (IsDamage && Target == EffectTargetType.AreaEnemy);
        private bool UsesConeAngle => IsDamage && Target == EffectTargetType.AreaEnemy &&
                                      Timing == EffectApplicationTiming.Immediate;
    }
}
