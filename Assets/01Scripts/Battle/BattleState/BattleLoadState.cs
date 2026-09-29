using Cysharp.Threading.Tasks;

namespace LAD.Battle {
	public sealed class BattleLoadState : BattleState {
		public BattleLoadState(BattleManager battleManager) : base(battleManager) { }

		public override void OnEnter() {
			LoadBattleAsync().Forget();
		}

		private async UniTask LoadBattleAsync() {
			await BattleManager.LoadBattle();
		}
	}
}
