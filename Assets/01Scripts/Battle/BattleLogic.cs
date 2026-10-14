using System;
using LARD.Player;
using LARD.Stat;

namespace LARD.Battle {
    public sealed class BattleLogic {
        private readonly BattleManager _battleManager;

        public BattleLogic(BattleManager battleManager) {
            _battleManager = battleManager ?? throw new ArgumentNullException(nameof(battleManager));
        }

        public void OnMonsterDied() {
            int kills = _battleManager.RegisterMonsterKill();
            BattleConstModel constants = BattleDataSet.Constants;
            if (constants == null || PlayerDataSet.Resource == null) {
                return;
            }

            if (constants.SilverKillsPerReward > 0 && constants.SilverRewardAmount > 0 &&
                kills % constants.SilverKillsPerReward == 0) {
                PlayerDataSet.Resource.Add(ResourceType.Silver, constants.SilverRewardAmount);
            }

            if (constants.GoldKillsPerReward > 0 && constants.GoldRewardAmount > 0 &&
                kills % constants.GoldKillsPerReward == 0) {
                PlayerDataSet.Resource.Add(ResourceType.Gold, constants.GoldRewardAmount);
            }
        }

        public bool OnStageTimeExpired(bool stageSpawnsCompleted) {
            if (_battleManager.State != BattleStateType.Playing ||
                (stageSpawnsCompleted && _battleManager.AliveMonsterCount == 0)) {
                return false;
            }

            _battleManager.CompleteDefeat();
            return true;
        }
    }
}
