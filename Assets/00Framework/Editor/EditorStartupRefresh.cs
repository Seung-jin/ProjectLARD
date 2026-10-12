using UnityEditor;

namespace LSJ.Editor {
    [InitializeOnLoad]
    internal static class EditorStartupRefresh {
        private const string RefreshedSessionKey = "LARD.EditorStartupRefresh.Refreshed";

        static EditorStartupRefresh() {
            EditorApplication.delayCall += RefreshOnce;
        }

        private static void RefreshOnce() {
            if (SessionState.GetBool(RefreshedSessionKey, false)) {
                return;
            }

            SessionState.SetBool(RefreshedSessionKey, true);
            AssetDatabase.Refresh();
        }
    }
}
