using LAD.Effects;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using LAD.Battle;
using UnityEngine;

namespace LAD.Skill {
    public enum EffectConditionType { None, TargetHealthBelowPercent, TargetHealthAbovePercent, TargetCrowdControlled }

    [Serializable]
    public sealed class EffectData {
        [Header("Trigger")]
        [SerializeField] private BattleObjEventType timing;
        [SerializeField, Min(0f)] private float interval;
        [SerializeField, Min(1)] private int repeatCount = 1;

        [Header("Target")]
        [SerializeField] private EffectTargetType target;
        [SerializeField, Min(0f)] private float radius;
        [SerializeField, Min(1)] private int maxTargetCount = 1;

        [Header("Effect")]
        [SerializeField] private EffectType effect;
        [SerializeField, Min(0f)] private float value;
        [SerializeField, Min(0f)] private float duration;
        [SerializeField] private DamageType damageType = DamageType.Magic;

        [Header("Condition")]
        [SerializeField] private EffectConditionType condition;
        [SerializeField, Range(0f, 1f)] private float conditionThreshold;

        [Header("Stack")]
        [Tooltip("같은 대상으로 들어온 효과가 같은 중첩인지 판별하는 키입니다. 비워두면 Effect 타입을 사용합니다.")]
        [SerializeField] private string stackKey;
        [SerializeField] private EffectStackPolicy stackPolicy = EffectStackPolicy.Independent;
        [SerializeField] private EffectDurationPolicy durationPolicy = EffectDurationPolicy.Refresh;
        [SerializeField] private EffectStackScope stackScope = EffectStackScope.SharedOnTarget;
        [SerializeField, Min(1)] private int maxStacks = 1;

        public BattleObjEventType Timing => timing;
        public float Interval => interval;
        public int RepeatCount => Mathf.Max(1, repeatCount);
        public EffectConditionType Condition => condition;
        public float ConditionThreshold => Mathf.Clamp01(conditionThreshold);
        public EffectTargetType Target => target;
        public float Radius => radius;
        public int MaxTargetCount => maxTargetCount;
        public EffectType Effect => effect;
        public float Value => value;
        public float Duration => duration;
        public DamageType DamageType => damageType;

        public bool MeetsCondition(BattleObj target) {
            if (target == null || target.StatController == null) return false;
            if (condition == EffectConditionType.None) return true;
            if (condition == EffectConditionType.TargetCrowdControlled)
                return target.BuffController != null && target.BuffController.HasCrowdControl(
                    CrowdControlType.Slow | CrowdControlType.Stun | CrowdControlType.Root);
            float maxHealth = target.StatController.GetStat(LAD.Stat.StatType.MaxHealth);
            float health = target.StatController.GetResource(LAD.Stat.ResourceType.Health);
            if (maxHealth <= 0f) return false;
            float ratio = health / maxHealth;
            return condition == EffectConditionType.TargetHealthBelowPercent
                ? ratio <= ConditionThreshold : ratio >= ConditionThreshold;
        }

        public bool HasPeriodicExecution => Interval > 0f && RepeatCount > 1;

        public async UniTask<bool> ActivateAsync(BattleObj source, BattleObj effectTarget, CancellationToken cancellationToken = default) {
            return await ActivateRepeatedAsync(source, effectTarget, 0, cancellationToken);
        }

        private async UniTask<bool> ActivateRepeatedAsync(BattleObj source, BattleObj effectTarget,
            int startIndex, CancellationToken cancellationToken) {
            int sourceLife = source != null ? source.LifeVersion : 0;
            int targetLife = effectTarget != null ? effectTarget.LifeVersion : 0;
            BattleManager manager = BattleManager.Current;
            int round = manager != null ? manager.CurrentRoundId : 0;
            bool activated = false;
            for (int index = startIndex; index < RepeatCount; index++) {
                if (index > 0 && Interval > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(Interval), cancellationToken: cancellationToken);
                if (cancellationToken.IsCancellationRequested) break;
                if (effectTarget == null || effectTarget.IsDead || effectTarget.LifeVersion != targetLife ||
                    (sourceLife != 0 && (source == null || source.IsDead || source.LifeVersion != sourceLife)) ||
                    (manager != null && (!manager.IsRoundActive || manager.CurrentRoundId != round))) break;
                activated |= SkillEffectFactory.Activate(this, source, effectTarget);
            }
            return activated;
        }
        public string StackKey => string.IsNullOrWhiteSpace(stackKey) ? effect.ToString() : stackKey;
        public EffectStackPolicy StackPolicy => stackPolicy;
        public EffectDurationPolicy DurationPolicy => durationPolicy;
        public EffectStackScope StackScope => stackScope;
        public int MaxStacks => Mathf.Max(1, maxStacks);

        public static EffectData CreateRuntime(
            EffectType effect,
            float value,
            float duration,
            string stackKey,
            EffectStackPolicy stackPolicy = EffectStackPolicy.Single,
            EffectDurationPolicy durationPolicy = EffectDurationPolicy.Refresh,
            EffectStackScope stackScope = EffectStackScope.PerSource,
            int maxStacks = 1, float interval = 0f, int repeatCount = 1) {
            return new EffectData {
                target = EffectTargetType.Enemy,
                maxTargetCount = 1,
                effect = effect,
                value = Mathf.Max(0f, value),
                duration = Mathf.Max(0f, duration),
                stackKey = stackKey,
                stackPolicy = stackPolicy,
                durationPolicy = durationPolicy,
                stackScope = stackScope,
                maxStacks = Mathf.Max(1, maxStacks),
                interval = Mathf.Max(0f, interval),
                repeatCount = Mathf.Max(1, repeatCount),
            };
        }

        /// <summary>이 데이터에 해당하는 효과 실행 스크립트를 찾아 대상에게 적용합니다.</summary>
        public bool Activate(BattleObj source, BattleObj effectTarget) {
            string sourceName = source != null ? source.name : "None";
            string targetName = effectTarget != null ? effectTarget.name : "None";
            Debug.Log(
                $"[Effect] 발동: Type={Effect}, Timing={Timing}, Source={sourceName}, " +
                $"Target={targetName}, Value={Value:0.###}, Duration={Duration:0.###}, StackKey={StackKey}",
                source != null ? source : effectTarget);

            if (HasPeriodicExecution) {
                bool first = SkillEffectFactory.Activate(this, source, effectTarget);
                if (first) ActivateRepeatedAsync(source, effectTarget, 1, default).Forget();
                return first;
            }
            bool activated = false;
            for (int index = 0; index < RepeatCount; index++) {
                activated |= SkillEffectFactory.Activate(this, source, effectTarget);
            }
            return activated;
        }
    }
}
