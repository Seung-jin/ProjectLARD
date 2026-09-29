using System;
using System.Collections.Generic;
using LAD.Article;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace LAD.Skill {
    public enum SkillTargetType {
        Monster,
        Point,
    }

    [Serializable]
    public sealed class SkillGradeData {
        [Header("Grade")]
        [SerializeField, Range(1, PlayerArticle.MaxGrade)] private int articleGrade = 1;
        [Header("Grade Values")]
        [OdinSerialize, DictionaryDrawerSettings(KeyLabel = "Key", ValueLabel = "Value")]
        private Dictionary<string, float> values = new Dictionary<string, float>();
        [Header("Grade Prefabs")]
        [Tooltip("이 등급에서 사용할 스킬 오브젝트 프리팹입니다. 비어 있으면 스킬이 즉시 발동합니다.")]
        [OdinSerialize, DictionaryDrawerSettings(KeyLabel = "Key", ValueLabel = "Prefab")]
        private Dictionary<string, SkillObjBase> prefabs = new Dictionary<string, SkillObjBase>();

        public int ArticleGrade => articleGrade;
        public IReadOnlyDictionary<string, float> Values => values;
        public IReadOnlyDictionary<string, SkillObjBase> Prefabs => prefabs;

        public bool TryGetValue(string key, out float value) {
            value = 0f;
            return !string.IsNullOrWhiteSpace(key) && values != null && values.TryGetValue(key, out value);
        }

        public bool TryGetPrefab(string key, out SkillObjBase prefab) {
            prefab = null;
            return !string.IsNullOrWhiteSpace(key) && prefabs != null && prefabs.TryGetValue(key, out prefab) &&
                   prefab != null;
        }
    }

    [CreateAssetMenu(fileName = "SkillModel", menuName = "LAD/Data/Skill Model")]
    public sealed class SkillModel : SerializedScriptableObject {
        [SerializeField, Min(0)] private int id;
        [SerializeField] private SkillTargetType targetType = SkillTargetType.Monster;
        [OdinSerialize] private List<SkillGradeData> grades = new List<SkillGradeData>();

        public int Id => id;
        public SkillTargetType TargetType => targetType;
        public IReadOnlyList<SkillGradeData> Grades => grades;

        public bool TryGetGrade(int articleGrade, out SkillGradeData gradeData) {
            if (grades != null) {
                for (int i = 0; i < grades.Count; i++) {
                    if (grades[i] != null && grades[i].ArticleGrade == articleGrade) {
                        gradeData = grades[i];
                        return true;
                    }
                }
            }
            gradeData = null;
            return false;
        }
    }
}
