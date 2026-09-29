using LAD.Effects;
using System.Collections.Generic;
using LAD.Battle;
using LAD.Skill.Effect;
using UnityEngine;

namespace LAD.Skill {
    /// <summary>효과 타입과 전용 실행 스크립트를 연결하는 진입점입니다.</summary>
    public static class SkillEffectFactory {
        private static readonly IReadOnlyDictionary<EffectType, SkillEffectBase> Effects =
            new Dictionary<EffectType, SkillEffectBase> {
                { EffectType.Damage, new DamageSkillEffect() },
                { EffectType.ArmorReduction, new ArmorReductionSkillEffect() },
                { EffectType.DamageTakenIncrease, new DamageTakenIncreaseSkillEffect() },
            };

        public static bool Activate(EffectData data, BattleObj source, BattleObj target) {
            if (data == null) {
                Debug.LogError("발동할 EffectData가 없습니다.");
                return false;
            }
            if (!data.MeetsCondition(target)) {
                return false;
            }
            switch (data.Effect) {
                case EffectType.PoisonDamage:
                case EffectType.MoveSpeedReduction:
                case EffectType.DamageReductionPoints:
                case EffectType.Stun:
                case EffectType.Root:
                    return target.Effects != null && target.Effects.Apply(data, source, data);
                case EffectType.LightningStrike:
                    return BattleLogic.ApplyDamage(data.Value, source, target, data.DamageType,
                        "번개 효과", AttackCategory.Effect) > 0f;
            }
            if (!Effects.TryGetValue(data.Effect, out SkillEffectBase effect)) {
                Debug.LogError($"{data.Effect}에 해당하는 효과 실행 스크립트가 등록되지 않았습니다.", source);
                return false;
            }

            bool succeeded = effect.Activate(data, source, target);
            if (!succeeded) {
                Debug.LogWarning($"[Effect] 적용 실패: Type={data.Effect}, StackKey={data.StackKey}", source);
            }
            return succeeded;
        }
    }
}
