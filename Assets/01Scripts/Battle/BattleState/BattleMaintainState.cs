namespace LAD.Battle {
	public sealed class BattleMaintainState : BattleState {
		public BattleMaintainState(BattleManager battleManager) : base(battleManager) { }

		public override void OnEnter() {
			BattleManager.EnterMaintainState();
		}
	}
}
