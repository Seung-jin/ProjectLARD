using UnityEngine;

namespace LAD.Battle {
	[CreateAssetMenu(fileName = "BattleConstModel", menuName = "LAD/Data/Battle Const Model")]
	public sealed class BattleConstModel : ScriptableObject {
		[SerializeField, Min(0)] private int startingGold = 3;
		[SerializeField, Min(1)] private int startingHealth = 3;
		[SerializeField, Min(1)] private int timeoutHealthDamage = 1;

		public int StartingGold => startingGold;
		public int StartingHealth => startingHealth;
		public int TimeoutHealthDamage => timeoutHealthDamage;
	}
}
