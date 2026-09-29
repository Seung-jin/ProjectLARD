using LAD.Article;
using LAD.Stat;
using UnityEngine;

namespace LAD.Battle {
	/// <summary>기물 개인 스탯과 BattleManager의 전역 각인 스탯을 합산해 조회합니다.</summary>
	public static class BattleStatLogic {
		public static float GetStat(BattleObj battleObj, StatType type) {
			float localValue = battleObj?.StatController?.GetStat(type) ?? 0f;
			BattleManager manager = BattleManager.Current;
			if (battleObj is not ArticleObj || manager == null) {
				return localValue;
			}

			return battleObj.StatController?.GetStat(type, manager.GlobalStatController) ?? localValue;
		}
	}
}
