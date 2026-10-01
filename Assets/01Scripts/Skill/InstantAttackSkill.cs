using UnityEngine;

namespace LARD.Skill {
    public abstract class InstantAttackSkill : SkillBase {
        [SerializeField, Min(0f)] private float _damage = 1f;

        public override bool TryUse(IAttackTarget target) {
            if (Owner == null || !Owner.isActiveAndEnabled || target == null ||
                (target is Object unityTarget && unityTarget == null) || !target.IsAlive || target.TargetTransform == null) {
                return false;
            }

            Animator animator = Owner.GraphicController?.Animator;
            if (animator != null) {
                foreach (AnimatorControllerParameter parameter in animator.parameters) {
                    if (parameter.name == SkillAnimationTriggerKeys.Attack && parameter.type == AnimatorControllerParameterType.Trigger) {
                        animator.SetTrigger(SkillAnimationTriggerKeys.Attack);
                        break;
                    }
                }
            }

            target.TakeDamage(_damage);
            return true;
        }
    }
}
