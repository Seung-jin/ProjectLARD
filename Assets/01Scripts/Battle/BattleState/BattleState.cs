using LSJ.State;

namespace LAD.Battle {
	public abstract class BattleState : BaseState<BattleStateType> {
		protected BattleManager BattleManager { get; }

		protected BattleState(BattleManager battleManager) {
			BattleManager = battleManager;
		}
	}
}
