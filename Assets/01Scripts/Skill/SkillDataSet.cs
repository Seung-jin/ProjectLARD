using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LAD.Data;
using LSJ.Util;
using UnityEngine;

namespace LAD.Skill {
    public sealed class SkillDataSet : IDataSet {
        public const string AddressableLabel = "Skill";

        private static readonly Dictionary<int, SkillModel> models = new Dictionary<int, SkillModel>();

        public async UniTask Load() {
            models.Clear();

            IList<ScriptableObject> assets = await AddressableManager.LoadAssetAsyncList<ScriptableObject>(
                new[] { AddressableLabel }, AddAsset);
            if (assets == null) {
                Debug.LogError($"SkillDataSet 로드에 실패했습니다. Addressable Label: {AddressableLabel}");
            }
        }

        public static bool TryGet(int skillId, out SkillModel model) {
            return models.TryGetValue(skillId, out model);
        }

        private static void AddAsset(ScriptableObject asset) {
            if (asset is SkillModel model) {
                if (!models.TryAdd(model.Id, model)) {
                    Debug.LogError($"SkillModel ID가 중복되었습니다: {model.Id} ({model.name})");
                }
            }
        }
    }
}
