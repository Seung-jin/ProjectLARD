using LARD.Skill;
using UnityEngine;

namespace LARD.Article {
    public sealed class SkillController : MonoBehaviour {
        public SkillModel Model { get; private set; }
        public SkillBase Skill { get; private set; }

        public bool TryUse() {
            if (Skill == null || Skill.Owner == null || Skill.Owner.Model == null) {
                return false;
            }

            var targets = FindTargetUtil.FindTargets(Skill.Owner.transform.position, Skill.Owner.Model.Range);
            return targets.Count > 0 && Skill.TryUse(targets[0]);
        }

        public void SetSkill(SkillModel model, SkillBase skill) {
            ClearSkill();
            Model = model;
            Skill = skill;
        }

        public void ClearSkill() {
            if (Skill != null) {
                Destroy(Skill);
            }

            Skill = null;
            Model = null;
        }

        private void OnDestroy() {
            ClearSkill();
        }
    }

}
