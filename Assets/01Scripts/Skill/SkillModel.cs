using System.Collections.Generic;
using UnityEngine;

namespace LARD.Skill {
	[CreateAssetMenu(fileName = "SkillModel", menuName = "Data/Skill/Model")]
	public sealed class SkillModel : ScriptableObject {
		public int Id;
		public List<EffectData> Effects = new();
	}
}
