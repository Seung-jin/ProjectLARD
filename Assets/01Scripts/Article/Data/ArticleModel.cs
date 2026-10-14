using Sirenix.OdinInspector;
using LARD.Skill;
using UnityEngine;

namespace LARD.Article {
	public enum ArticleRace {
		Beastkin = 0,
		Warrior = 2,
		Mage = 1,
	}

	// 기물의 기본 능력치와 스킬 데이터
	[CreateAssetMenu(fileName = "ArticleModel", menuName = "Data/Article/Model")]
	public sealed class ArticleModel : ScriptableObject {
		public int Id;
		public ArticleRace Race;
		[Min(1)] public int Tier = 1;
		[Min(0), Tooltip("2티어 이상 승급 시 연결할 같은 직업의 다음 티어 모델 ID. 0이면 승급할 수 없습니다.")]
		public int NextTierModelId;
		[Min(0f)] public float AttackSpeed = 1f;
		[Min(0f)] public float MoveSpeed = 3f;
		[Min(0f)] public float Range = 5f;

		[AssetSelector] public ArticleObj Prefab;
		[AssetSelector] public SkillModel Skill;
	}
}
