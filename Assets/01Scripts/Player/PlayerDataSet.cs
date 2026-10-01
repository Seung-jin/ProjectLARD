using Cysharp.Threading.Tasks;
using LARD.Data;

namespace LARD.Player {
    public sealed class PlayerDataSet : IDataSet {
        public static PlayerResource Resource { get; private set; }

        public UniTask Load() {
            Resource ??= new PlayerResource();
            return UniTask.CompletedTask;
        }
    }
}
