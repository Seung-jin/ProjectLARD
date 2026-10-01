using Sirenix.OdinInspector;
using UnityEngine;

namespace LARD.Skill {
    public enum SkillTargetType {
    }

    public sealed class SkillGradeData {
    }

    [CreateAssetMenu(fileName = "SkillModel", menuName = "Data/Skill/Model")]
    public sealed class SkillModel : ScriptableObject {
        public int Id;

        [AssetSelector] public SkillBase Skill;
    }

}
