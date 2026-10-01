using UnityEngine;

namespace LARD.Skill {
    [CreateAssetMenu(fileName = "SylinMissileAttack", menuName = "Data/Skill/Sylin Missile Attack")]
    public sealed class SylinMissileAttackSkill : SkillBase {
        [SerializeField, Min(0f)] private float _damage = 1f;
        [SerializeField, Min(0.01f)] private float _missileSpeed = 6f;

        public override bool TryUse(IAttackTarget target) {
            if (Owner == null || !Owner.isActiveAndEnabled || target == null ||
                (target is Object unityTarget && unityTarget == null) || !target.IsAlive || target.TargetTransform == null) {
                return false;
            }

            GameObject missileObject = new GameObject("Sylin Magic Missile");
            missileObject.transform.position = Owner.transform.position;
            MagicMissile missile = missileObject.AddComponent<MagicMissile>();
            missile.Initialize(Owner, target, _damage, _missileSpeed);
            return true;
        }
    }
}
