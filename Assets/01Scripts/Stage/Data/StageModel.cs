using UnityEngine;

namespace LARD.Stage {
    [CreateAssetMenu(fileName = "StageModel", menuName = "Data/Stage/Model")]
    public sealed class StageModel : ScriptableObject {
        [Min(1)] public int Id = 1;
        [Min(0f)] public float StageTime = 120f;
        [Min(1)] public int MonsterCount = 60;
        [Min(1)] public int MonsterId = 1;
        [Min(1f)] public float MonsterHealth = 200f;
        public int BossMonsterId = 0;
        [Min(0f)] public float BossMonsterHealth;
    }
}
