namespace LAD.Battle {
	public sealed class BattleStartState : BattleState {
		public BattleStartState(BattleManager battleManager) : base(battleManager) { }

		public override void OnEnter() {
			BattleManager.StartBattle();
		}
	}
}
