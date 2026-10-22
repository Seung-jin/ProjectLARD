using LARD.Article;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ArticlePrefabRepairBatch {
    private const string SessionKey = "LARD.ArticlePrefabRepairBatch.Ran";

    static ArticlePrefabRepairBatch() {
        EditorApplication.delayCall += RepairOncePerSession;
    }

    private static void RepairOncePerSession() {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);
        Run();
    }

    [MenuItem("Tools/LARD/Repair Article Prefab References")]
    public static void Run() {
        const string modelFolder = "Assets/04Data/Article/Article";
        const string prefabFolder = "Assets/03Resources/Article/Prefab";
        int checkedCount = 0;
        int repairedCount = 0;
        int missingCount = 0;
        var modelPaths = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:ArticleModel", new[] { modelFolder })) {
            string modelPath = AssetDatabase.GUIDToAssetPath(guid);
            ArticleModel model = AssetDatabase.LoadAssetAtPath<ArticleModel>(modelPath);
            if (model == null) continue;

            string prefabPath = $"{prefabFolder}/ArticleObj_{model.Id}.prefab";
            GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            ArticleObj prefab = prefabObject != null ? prefabObject.GetComponent<ArticleObj>() : null;
            if (prefab == null) {
                Debug.LogError($"[ArticlePrefabRepair] Missing ArticleObj prefab for model {model.Id}: {prefabPath}");
                missingCount++;
                continue;
            }

            checkedCount++;
            if (model.Id < 10) {
                if (model.Prefab != prefab) {
                    Debug.LogWarning($"[ArticlePrefabRepair] Check model {model.Id}: prefab reference does not match {prefabPath}");
                }
                continue;
            }

            modelPaths.Add(modelPath);
            if (model.Prefab != prefab) repairedCount++;
            model.Prefab = prefab;
            EditorUtility.SetDirty(model);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.ForceReserializeAssets(modelPaths);
        Debug.Log($"[ArticlePrefabRepair] Checked {checkedCount}, repaired {repairedCount}, missing {missingCount}.");
        if (missingCount > 0 || checkedCount != 21) {
            throw new System.InvalidOperationException("Article prefab reference validation failed.");
        }
    }
}
