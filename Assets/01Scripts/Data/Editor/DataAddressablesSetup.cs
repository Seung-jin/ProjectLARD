using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using LARD.Article;
using LARD.Shop;
using LARD.Upgrade;

namespace LARD.Data.Editor {
    public static class DataAddressablesSetup {
        private const string DataRoot = "Assets/04Data/";

        [InitializeOnLoadMethod]
        private static void RegisterAfterReload() {
            EditorApplication.delayCall += Register;
        }

        [MenuItem("Tools/LARD/Register Data Addressables")]
        public static void Register() {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null || settings.DefaultGroup == null) {
                Debug.LogWarning("[DataAddressables] Addressables settings are unavailable.");
                return;
            }

            bool changed = RegisterFolder(settings, "ShopDataSet", ShopDataSet.AddressableLabel);
            changed |= RegisterFolder(settings, "ArticleDataSet", ArticleDataSet.AddressableLabel);
            changed |= RegisterFolder(settings, "Upgrade", UpgradeDataSet.AddressableLabel);
            if (changed) {
                AssetDatabase.SaveAssets();
            }
        }

        private static bool RegisterFolder(AddressableAssetSettings settings, string folderName, string label) {
            string folder = DataRoot + folderName;
            if (!AssetDatabase.IsValidFolder(folder)) {
                return false;
            }

            bool changed = false;
            if (!settings.GetLabels().Contains(label)) {
                settings.AddLabel(label);
                changed = true;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { folder })) {
                AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                if (entry == null) {
                    entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                    changed = true;
                }

                if (folderName != label) {
                    changed |= entry.SetLabel(folderName, false);
                }
                changed |= entry.SetLabel(label, true);
            }

            return changed;
        }
    }
}
