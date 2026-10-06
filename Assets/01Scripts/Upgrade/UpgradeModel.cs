using LARD.Article;
using UnityEngine;

namespace LARD.Upgrade {
    [CreateAssetMenu(fileName = "UpgradeModel", menuName = "Data/Upgrade/Model")]
    public sealed class UpgradeModel : ScriptableObject {
        public ArticleRace Race;
        [Min(0)] public int Level;
        [Min(0f), Tooltip("Attack power increase in percent (10 = 10%).")]
        public float AttackPowerIncreasePercent;
        [Min(0)] public int NextLevelCost;
    }
}
