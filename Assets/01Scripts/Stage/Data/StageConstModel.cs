using UnityEngine;

namespace LARD.Stage {
    [CreateAssetMenu(fileName = "StageConstModel", menuName = "Data/Stage/Constants")]
    public sealed class StageConstModel : ScriptableObject {
        [Min(0f)] public float StageTime = 120f;
        [Min(1)] public int MonsterCount = 60;
    }
}
