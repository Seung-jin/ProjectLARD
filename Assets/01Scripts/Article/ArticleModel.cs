using System.Collections.Generic;
using LAD.Stat;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace LAD.Article {
    [CreateAssetMenu(fileName = "ArticleModel", menuName = "LAD/Data/Article Model")]
    public sealed class ArticleModel : SerializedScriptableObject {
        [SerializeField, Min(0)] private int id;
        [SerializeField, Range(1, 5)] private int tier = 1;
        [SerializeField, Min(0)] private int price = 1;
        [SerializeField, Min(0)] private int basicAttackId;
        [SerializeField, Min(0)] private int skillId;
        [SerializeField] private ArticleObj prefab;
        [OdinSerialize, DictionaryDrawerSettings(KeyLabel = "스탯", ValueLabel = "등급별 값")]
        private Dictionary<StatType, StatData> stats = new Dictionary<StatType, StatData>();

        public int Id => id;
        public int Tier => tier;
        public int Price => price;
        public int BasicAttackId => basicAttackId;
        public int SkillId => skillId;
        public ArticleObj Prefab => prefab;
        public IReadOnlyDictionary<StatType, float> GetStats(int grade) {
            var result = new Dictionary<StatType, float>();
            foreach (var keyValuePair in stats) {
                StatType statType = keyValuePair.Key;
                StatData statData = keyValuePair.Value;
                result[statType] = statData.GetValue(grade);
            }

            return result;
        }
    }
}
