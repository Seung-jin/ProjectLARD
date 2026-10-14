using System.Collections.Generic;
using UnityEngine;

namespace LARD.Skill {
	[CreateAssetMenu(fileName = "SkillModel", menuName = "Data/Skill/Model")]
	public sealed class SkillModel : ScriptableObject {
		public int Id;
		public SkillObjBase SkillObjectPrefab;
		[Min(0f), Tooltip("Fixed damage dealt by each damage effect hit before upgrades, buffs, and defense.")]
		public float Damage;
		public List<EffectData> Effects = new();
	}
}
