using System;
using LARD.Player;
using LARD.Stat;

namespace LARD.Battle {
    public sealed class BattleLogic {
        private const int KillsPerGold = 10;
        private readonly BattleManager _battleManager;

        public BattleLogic(BattleManager battleManager) {
            _battleManager = battleManager ?? throw new ArgumentNullException(nameof(battleManager));
        }

        public void OnMonsterDied() {
            int kills = _battleManager.RegisterMonsterKill();
            if (kills % KillsPerGold == 0) {
                PlayerDataSet.Resource.Add(ResourceType.Gold, 1);
            }
        }
    }
}
