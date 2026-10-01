using Sirenix.OdinInspector;
using LARD.Skill;
using UnityEngine;

namespace LARD.Article {
	public enum ArticleRace {
		Yoz,
		Sylin,
		Shushire,
	}

	[CreateAssetMenu(fileName = "ArticleModel", menuName = "Data/Article/Model")]
	public sealed class ArticleModel : ScriptableObject {
		public int Id;
		public ArticleRace Race;
		[Min(1)] public int Tier = 1;
		[Min(0f)] public float AttackPower = 100f;
		[Min(0f)] public float AttackSpeed = 1f;
		[Min(0f)] public float MoveSpeed = 3f;
		[Min(0f)] public float Range = 5f;

		[AssetSelector] public ArticleObj Prefab;
		[AssetSelector] public SkillModel Skill;
	}
}
