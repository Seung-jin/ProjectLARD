using System.Collections.Generic;
using UnityEngine;

namespace LARD.Skill {
	[CreateAssetMenu(fileName = "SkillModel", menuName = "Data/Skill/Model")]
	public sealed class SkillModel : ScriptableObject {
		public int Id;
		[Tooltip("Damage effects use a multiplier of the owner's attack power (1 = 100%).")]
		public List<EffectData> Effects = new();
	}
}
