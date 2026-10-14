using UnityEngine;

namespace LARD.Battle {
    [CreateAssetMenu(fileName = "BattleConstModel", menuName = "Data/Battle/Constants")]
    public sealed class BattleConstModel : ScriptableObject {
        [Min(1)] public int SilverKillsPerReward = 2;
        [Min(0)] public int SilverRewardAmount = 1;
        [Min(1)] public int GoldKillsPerReward = 5;
        [Min(0)] public int GoldRewardAmount = 1;
    }
}
