using System;
using LARD.Article;
using UnityEngine;

namespace LARD.Skill {
    public sealed class SkillFactory {
        public SkillBase Create(SkillModel model, ArticleObj owner) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }

            SkillBase skill = ScriptableObject.CreateInstance<SkillBase>();
            skill.Initialize(owner, model);
            return skill;
        }
    }

}
