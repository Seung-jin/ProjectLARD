using System;
using LARD.Player;
using LARD.Stage;
using LARD.Stat;

namespace LARD.Battle {
    /// <summary>
    /// 몬스터 처치 보상과 스테이지 시간 만료 시 패배 조건을 판정한다.
    /// </summary>
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

            CompleteDefeat();
            return true;
        }

        /// <summary>패배 처리를 한곳에서 실행한다. 결과 UI도 이 함수에 연결한다.</summary>
        private void CompleteDefeat() {
            StageLogic.CompleteDefeat();
            _battleManager.CompleteDefeat();
        }
    }
}
