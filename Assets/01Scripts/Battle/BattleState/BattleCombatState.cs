namespace LAD.Battle {
	public sealed class BattleCombatState : BattleState {
		public BattleCombatState(BattleManager battleManager) : base(battleManager) { }

		public override void OnEnter() {
			BattleManager.StartCurrentRound();
		}

		public override void OnUpdate() {
			BattleManager.UpdateCurrentRound(UnityEngine.Time.deltaTime);
		}
	}
}
