using UnityEngine;

namespace LARD.Stage {
    [CreateAssetMenu(fileName = "StageModel", menuName = "Data/Stage/Model")]
    public sealed class StageModel : ScriptableObject {
        [Min(1)] public int Id = 1;
        [Min(1)] public int MonsterId = 1;
        [Min(0f), Tooltip("0 uses the MonsterModel health.")] public float MonsterHealth;
    }
}
