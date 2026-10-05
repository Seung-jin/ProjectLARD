using UnityEngine;

namespace LARD.Stage {
    [CreateAssetMenu(fileName = "StageModel", menuName = "Data/Stage/Model")]
    public sealed class StageModel : ScriptableObject {
        [Min(1)] public int Id = 1;
        [Min(1)] public int MonsterId = 1;
        [Min(0)] public int MonsterCount;
        [Min(0f)] public float StageTime;
    }
}
