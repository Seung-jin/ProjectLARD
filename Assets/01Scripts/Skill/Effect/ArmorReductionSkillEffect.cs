using System.Collections.Generic;
using LAD.Battle;
using LAD.Buff;
using LAD.Stat;
using UnityEngine;

namespace LAD.Skill.Effect {
    public sealed class ArmorReductionSkillEffect : SkillEffectBase {
        public override bool Activate(EffectData data, BattleObj source, BattleObj target) {
            if (target?.BuffController == null) {
                Debug.LogWarning("[Effect:ArmorReduction] 대상의 BuffController가 없습니다.", target);
                return false;
            }

            string buffId = BuildBuffId(data, source);
            var modifiers = new List<StatModifier> {
                new StatModifier(StatType.DamageReduction, 0f, -Mathf.Abs(data.Value)),
            };
            target.BuffController.AddBuff(
                new LAD.Buff.Buff(buffId, BuffType.Debuff, data.Duration, modifiers),
                data.StackPolicy, data.DurationPolicy, data.MaxStacks);
            Debug.Log(
                $"[Effect:ArmorReduction] 적용 완료: Target={target.name}, " +
                $"Ratio={Mathf.Abs(data.Value):P0}, Duration={data.Duration:0.##}, BuffId={buffId}",
                target);
            return true;
        }

        private static string BuildBuffId(EffectData data, BattleObj source) {
            string id = $"SkillEffect.{data.StackKey}";
            if (data.StackScope == EffectStackScope.PerSource && source != null) {
                id += $".Source{source.GetEntityId()}";
            }
            return id;
        }
    }
}
