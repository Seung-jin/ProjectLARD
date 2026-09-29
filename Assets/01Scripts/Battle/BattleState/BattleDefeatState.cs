namespace LAD.Battle {
	public sealed class BattleDefeatState : BattleState {
		public BattleDefeatState(BattleManager battleManager) : base(battleManager) { }

		public override void OnEnter() {
			BattleManager.CompleteRoundResult(BattleStateType.Defeat);
		}
	}
}
