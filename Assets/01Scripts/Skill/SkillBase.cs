using LARD.Article;
using UnityEngine;

namespace LARD.Skill {
    public static class SkillAnimationTriggerKeys {
        public const string Attack = "Attack";
    }

    [CreateAssetMenu(fileName = "Skill", menuName = "Data/Skill/Base")]
    public class SkillBase : ScriptableObject {
        public ArticleObj Owner { get; private set; }

        public virtual void Initialize(ArticleObj owner) {
            Owner = owner;
        }

        public virtual bool TryUse(IAttackTarget target) {
            return false;
        }
    }

}
