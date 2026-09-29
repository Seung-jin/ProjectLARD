using System;
using System.Collections.Generic;
using System.Text;
using LAD.Accessory;
using LAD.Article;
using LAD.Engraving;
using LAD.Player;
using LAD.Stat;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LAD.Battle {
    /// <summary>공격 시점의 값만 보관합니다. 풀링된 기물이나 변경 가능한 장비 참조는 보관하지 않습니다.</summary>
    public static class BattleCombatLog {
        public sealed class Entry {
            [ShowInInspector, ReadOnly] public long Id { get; internal set; }
            [ShowInInspector, ReadOnly] public string Name { get; internal set; }
            [ShowInInspector, ReadOnly] public string Title { get; internal set; }
            [ShowInInspector, ReadOnly, MultiLineProperty] public string Damage { get; internal set; }
            [ShowInInspector, ReadOnly, MultiLineProperty] public string Stats { get; internal set; }
            [ShowInInspector, ReadOnly, MultiLineProperty] public string Accessories { get; internal set; }
            [ShowInInspector, ReadOnly, MultiLineProperty] public string Engravings { get; internal set; }
        }

        public const int Capacity = 500;
        private static readonly List<Entry> entries = new List<Entry>();
        public static IReadOnlyList<Entry> Entries { get; } = entries.AsReadOnly();
        public static bool Recording { get; set; } = true;
        private static long nextId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() {
            Clear();
            Recording = true;
        }

        [Button("로그 삭제")]
        public static void Clear() => entries.Clear();

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void RecordDamage(BattleObj attacker, IDamageable target, string kind, DamageType type,
                                       float raw, float beforeDefense, float final, bool critical) {
            if (!Recording || attacker == null) return;
            Entry entry = Capture(attacker, kind);
            string targetName = target is UnityEngine.Object obj ? obj.name : target.GetType().Name;
            entry.Damage = $"대상 : {targetName}\n피해 유형 : {type}\n" +
                $"최종 대미지 (방어 적용 후, 보호막 흡수 전) : {final:0.###}\n" +
                $"크리티컬 여부 : {(critical ? "예" : "아니오")}\n" +
                $"입력 대미지 (공격/스킬 계수 적용 후) : {raw:0.###}\n" +
                $"대미지 증가 : {BattleStatLogic.GetStat(attacker, StatType.DamageIncrease):P2}\n" +
                $"치명타/증가 적용 후 : {beforeDefense:0.###}\n" +
                $"대상 피해 감소 : {Mathf.Clamp01(target.StatController.GetStat(StatType.DamageReduction) / 100f):P2}\n" +
                $"대상 받는 피해 증가 : {target.StatController.GetStat(StatType.DamageTakenIncrease):P2}\n" +
                $"대상 보호막 (피격 전) : {target.StatController.GetResource(ResourceType.Shield):0.###}\n" +
                $"대상 체력 (피격 전) : {target.StatController.GetResource(ResourceType.Health):0.###}" +
                (type == DamageType.True ? "\n고정 피해: 대상 피해 감소/받는 피해 증가 미적용" : "") +
                "\n\n대상에게 적용된 스탯 보정\n" + CaptureModifiers(target.StatController);
            Add(entry);
        }

        // 피해가 없는 버프/강화 스킬과 투사체 발사도 발동 시점에 남깁니다.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void RecordSkill(BattleObj attacker, string skillName, int activationIndex) {
            if (!Recording || attacker == null) return;
            Entry entry = Capture(attacker, "스킬 실행 시도");
            entry.Damage = $"스킬 : {skillName}\n실행 순번 : {activationIndex + 1}\n" +
                "실행 직전 기록입니다. 대상 부재 등으로 실행이 실패할 수도 있습니다. 피해가 발생하면 대상별 피해 로그에 결과가 기록됩니다.";
            Add(entry);
        }

        private static void Add(Entry entry) {
            if (entries.Count >= Capacity) entries.RemoveAt(0);
            entries.Add(entry);
        }

        private static Entry Capture(BattleObj attacker, string kind) {
            var article = attacker as ArticleObj;
            string displayName = attacker.name;
            string identity = article?.PlayerArticle != null
                ? $"기물 #{article.PlayerArticle.InstanceId}" : $"오브젝트 #{attacker.GetEntityId()}";
            return new Entry {
                Id = ++nextId,
                Name = displayName,
                Title = $"[{Time.time:0.00}s] {displayName}의 {kind} ({identity})",
                Stats = CaptureStats(attacker),
                Accessories = CaptureAccessories(article),
                Engravings = CaptureEngravings(article)
            };
        }

        private static string CaptureStats(BattleObj attacker) {
            var text = new StringBuilder("최종값 | 기본값 / 개인 적용값 / 전역 가산값\n");
            foreach (StatType type in Enum.GetValues(typeof(StatType))) {
                float local = attacker.StatController?.GetStat(type) ?? 0f;
                float final = BattleStatLogic.GetStat(attacker, type);
                text.AppendLine($"{StatName(type)} : {Format(type, final)} | " +
                    $"{Format(type, attacker.StatController?.GetBaseStat(type) ?? 0f)} / " +
                    $"{Format(type, local)} / {Format(type, final - local)}");
            }
            text.AppendLine($"실제 크리티컬 확률 : {BattleLogic.GetCriticalChance(attacker):P2}");
            text.AppendLine($"크리티컬 피해 배율 : {BattleLogic.GetCriticalDamageMultiplier(attacker):0.###}배");
            int activationCount = attacker is ArticleObj
                ? Mathf.Max(1, 1 + Mathf.FloorToInt(BattleStatLogic.GetStat(attacker, StatType.SkillActivationCount)))
                : 1;
            text.AppendLine($"스킬 실행 횟수 : {activationCount}회");
            text.AppendLine("스킬별 피해 계수는 입력 대미지에 포함됩니다. 개인 적용값에는 악세사리와 버프가 포함됩니다.");
            text.AppendLine("\n개인 스탯 보정 출처");
            text.Append(CaptureModifiers(attacker.StatController));
            return text.ToString();
        }

        private static string CaptureModifiers(StatController stats) {
            var text = new StringBuilder();
            if (stats == null) return "없음";
            foreach (var pair in stats.EnumerateModifiers()) {
                if (pair.Value.Count == 0) continue;
                string source = pair.Key is LAD.Buff.Buff buff ? buff.Id :
                    pair.Key is string key ? key : pair.Key.GetType().Name;
                if (source == "Object") source = "장비 등 개인 보정";
                text.AppendLine(source);
                foreach (StatModifier modifier in pair.Value)
                    text.AppendLine($"  {StatName(modifier.Type)} : 가산 {Format(modifier.Type, modifier.FlatValue)} / 비율 {modifier.PercentValue:P2}");
            }
            return text.Length == 0 ? "없음" : text.ToString();
        }

        private static string CaptureAccessories(ArticleObj article) {
            var text = new StringBuilder();
            foreach (AccessoryParts parts in Enum.GetValues(typeof(AccessoryParts))) {
                string partName = parts == AccessoryParts.Necklace ? "목걸이" : parts == AccessoryParts.Earring ? "귀걸이" : "반지";
                if (article?.PlayerArticle != null && PlayerDataSet.PlayerAccessories != null &&
                    PlayerDataSet.PlayerAccessories.TryGetEquipped(article.PlayerArticle.InstanceId, parts, out var accessory) &&
                    AccessoryDataSet.TryGet(accessory.AccessoryId, out var model)) {
                    StatModifier modifier = AccessoryEffectLogic.ToModifier(model);
                    text.AppendLine($"{partName} : {model.name} ({model.Grade})\n" +
                        $"  {StatName(modifier.Type)} {Format(modifier.Type, modifier.FlatValue)} / 비율 {modifier.PercentValue:P2}");
                } else text.AppendLine($"{partName} : 미장착");
            }
            return text.ToString();
        }

        private static string CaptureEngravings(ArticleObj article) {
            if (article == null) return "아군 기물이 아니므로 각인 효과 미적용";
            var text = new StringBuilder();
            foreach (EngravingBase engraving in EngravingLogic.AcquiredEngravings) {
                if (!engraving.IsAcquired) continue;
                text.AppendLine($"{engraving.Model.name} (ID {engraving.Id}, {engraving.Model.Grade})");
                foreach (EngravingEffectData effect in engraving.Model.Effects) {
                    string effectName = effect.Effect == LAD.Effects.EffectType.SkillRepeatCount ? "스킬 추가 실행" : effect.Effect.ToString();
                    string value = effect.Effect == LAD.Effects.EffectType.DamageIncrease ||
                        effect.Effect == LAD.Effects.EffectType.DamageTakenIncrease ? effect.Value.ToString("P2") : effect.Value.ToString("0.###");
                    text.AppendLine($"  {effectName} : {value}");
                }
            }
            return text.Length == 0 ? "적용 중인 각인 없음" : text.ToString();
        }

        private static string Format(StatType type, float value) {
            return type == StatType.CriticalChance || type == StatType.CriticalDamageIncrease ||
                type == StatType.DamageIncrease || type == StatType.DamageTakenIncrease
                ? value.ToString("P2") : value.ToString("0.###");
        }

        private static string StatName(StatType type) => type switch {
            StatType.MaxHealth => "최대 체력", StatType.MoveSpeed => "이동 속도",
            StatType.Swiftness => "신속", StatType.Strength => "힘", StatType.Dexterity => "민첩",
            StatType.Intelligence => "지능", StatType.Specialization => "특화", StatType.Critical => "치명",
            StatType.CriticalChance => "추가 크리티컬 확률", StatType.CriticalDamageIncrease => "크리티컬 피해 증가",
            StatType.DamageIncrease => "대미지 증가", StatType.DamageTakenIncrease => "받는 피해 증가",
            StatType.DamageReduction => "피해 감소 (%)", StatType.Range => "사거리",
            StatType.MaxMana => "최대 마나", StatType.ManaRegen => "마나 회복", _ => type.ToString()
        };
    }
}
