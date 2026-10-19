using System.Collections.Generic;
using UnityEngine;

namespace LARD.Skill {
	/// <summary>
	/// 스킬 식별자, 생성할 오브젝트와 순서대로 평가할 이펙트 설정을 보관하는 에셋이다.
	/// </summary>
	[CreateAssetMenu(fileName = "SkillModel", menuName = "Data/Skill/Model")]
	public sealed class SkillModel : ScriptableObject {
		public int Id;
		public SkillObjBase SkillObjectPrefab;
		public List<EffectData> Effects = new();
	}
}
