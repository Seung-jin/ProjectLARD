using UnityEngine;

namespace LARD.Skill {
    public interface IAttackTarget {
        Transform TargetTransform { get; }
        bool IsAlive { get; }
        void TakeDamage(float damage);
    }
}
