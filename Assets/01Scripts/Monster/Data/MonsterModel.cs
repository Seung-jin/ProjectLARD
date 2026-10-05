using Sirenix.OdinInspector;
using UnityEngine;

namespace LARD.Monster {
    [CreateAssetMenu(fileName = "MonsterModel", menuName = "Data/Monster/Model")]
    public sealed class MonsterModel : ScriptableObject {
        public int Id;
        [Min(0f)] public float Health = 100f;
        [Min(0f)] public float MoveSpeed = 3f;
        [Min(0f)] public float Defense;
        [AssetSelector] public MonsterObj Prefab;
    }
}