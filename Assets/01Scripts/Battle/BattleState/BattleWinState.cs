namespace LAD.Battle {
	public sealed class BattleWinState : BattleState {
		public BattleWinState(BattleManager battleManager) : base(battleManager) { }

		public override void OnEnter() {
			BattleManager.CompleteRoundResult(BattleStateType.Win);
		}
	}
}
