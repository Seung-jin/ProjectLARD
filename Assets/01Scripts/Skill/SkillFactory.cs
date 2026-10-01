using System;
using LARD.Article;
using UnityEngine;

namespace LARD.Skill {
    public sealed class SkillFactory {
        public SkillBase Create(SkillModel model, ArticleObj owner) {
            if (model == null || model.Skill == null) {
                throw new ArgumentException("SkillModel must reference a SkillBase asset.", nameof(model));
            }

            SkillBase skill = UnityEngine.Object.Instantiate(model.Skill);
            skill.Initialize(owner);
            return skill;
        }
    }

}
