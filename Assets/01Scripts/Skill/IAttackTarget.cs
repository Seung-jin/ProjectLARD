using UnityEngine;

namespace LARD.Skill {
	public interface IAttackTarget {
		Transform TargetTransform { get; }
		bool IsAlive { get; }
		float DefensePercent { get; }
		void TakeDamage(float damage);
	}
}
