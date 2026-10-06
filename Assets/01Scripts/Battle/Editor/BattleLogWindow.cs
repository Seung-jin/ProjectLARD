using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace LARD.Battle.Editor {
    public sealed class BattleLogWindow : OdinEditorWindow {
        [ShowInInspector, LabelText("오브젝트 아이디 필터 사용"), OnValueChanged(nameof(Refresh))]
        private bool _useObjectIdFilter;

        [ShowInInspector, LabelText("오브젝트 아이디"), ShowIf(nameof(_useObjectIdFilter)),
         OnValueChanged(nameof(Refresh))]
        private int _objectId;

        [ShowInInspector, ReadOnly, LabelText("표시 / 전체")]
        private string Count => $"{_visibleEntries.Count} / {BattleLog.Entries.Count}";

        [ShowInInspector, TableList(IsReadOnly = true, AlwaysExpanded = true), LabelText("전투 로그")]
        private List<BattleLogRow> _visibleEntries = new();

        [MenuItem("Tools/LARD/Battle Log")]
        private static void Open() {
            GetWindow<BattleLogWindow>("Battle Log").Show();
        }

        protected override void OnEnable() {
            base.OnEnable();
            BattleLog.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() {
            BattleLog.Changed -= Refresh;
        }

        [Button("로그 클리어"), PropertyOrder(-1)]
        private void ClearLog() {
            BattleLog.Clear();
        }

        private void Refresh() {
            _visibleEntries.Clear();
            foreach (BattleLogEntry entry in BattleLog.Entries) {
                if (_useObjectIdFilter && entry.ArticleObjectId != _objectId &&
                    entry.MonsterObjectId != _objectId) {
                    continue;
                }

                _visibleEntries.Add(new BattleLogRow(entry));
            }

            Repaint();
        }

        private sealed class BattleLogRow {
            [ShowInInspector, HideLabel, TableColumnWidth(60), LabelText("시간")]
            public readonly float 시간;
            [ShowInInspector, HideLabel, TableColumnWidth(130), LabelText("기물 오브젝트 아이디")]
            public readonly int 기물_오브젝트_아이디;
            [ShowInInspector, HideLabel, TableColumnWidth(80), LabelText("기물 아이디")]
            public readonly int 기물_아이디;
            [ShowInInspector, HideLabel, TableColumnWidth(90), LabelText("기본 공격력")]
            public readonly float 기본_공격력;
            [ShowInInspector, HideLabel, TableColumnWidth(70), LabelText("종족")]
            public readonly string 종족;
            [ShowInInspector, HideLabel, TableColumnWidth(120), LabelText("업그레이드 레벨")]
            public readonly int 업그레이드_레벨;
            [ShowInInspector, HideLabel, TableColumnWidth(110), LabelText("업그레이드 비율(%)")]
            public readonly float 업그레이드_비율;
            [ShowInInspector, HideLabel, TableColumnWidth(90), LabelText("기본 대미지")]
            public readonly float 기본_대미지;
            [ShowInInspector, HideLabel, TableColumnWidth(90), LabelText("최종 대미지")]
            public readonly float 최종_대미지;
            [ShowInInspector, HideLabel, TableColumnWidth(140), LabelText("몬스터 오브젝트 아이디")]
            public readonly int 몬스터_오브젝트_아이디;
            [ShowInInspector, HideLabel, TableColumnWidth(90), LabelText("몬스터 아이디")]
            public readonly int 몬스터_아이디;
            [ShowInInspector, HideLabel, TableColumnWidth(80), LabelText("방어력(%)")]
            public readonly float 방어력;
            [ShowInInspector, HideLabel, TableColumnWidth(100), LabelText("피격 전 체력")]
            public readonly float 피격_전_체력;
            [ShowInInspector, HideLabel, TableColumnWidth(100), LabelText("피격 후 체력")]
            public readonly float 피격_후_체력;

            public BattleLogRow(BattleLogEntry entry) {
                시간 = entry.Time;
                기물_오브젝트_아이디 = entry.ArticleObjectId;
                기물_아이디 = entry.ArticleId;
                기본_공격력 = entry.AttackPower;
                종족 = entry.Race;
                업그레이드_레벨 = entry.UpgradeLevel;
                업그레이드_비율 = entry.UpgradePercent;
                기본_대미지 = entry.BaseDamage;
                최종_대미지 = entry.FinalDamage;
                몬스터_오브젝트_아이디 = entry.MonsterObjectId;
                몬스터_아이디 = entry.MonsterId;
                방어력 = entry.Defense;
                피격_전_체력 = entry.HealthBefore;
                피격_후_체력 = entry.HealthAfter;
            }
        }
    }
}
