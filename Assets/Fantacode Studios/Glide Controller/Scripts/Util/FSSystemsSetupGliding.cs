#if UNITY_EDITOR
using UnityEditor;

namespace FS_ThirdPerson
{
    public partial class FSSystemsSetup
    {
        public static FSSystemInfo GlidingSystemSetup = new FSSystemInfo
        (
            characterType: CharacterType.Player,
            enabled: false,
            name: "Gliding System",
            prefabName: "Gliding Controller",
            welcomeEditorShowKey: "GlidingSystem_WelcomeWindow_Opened",
            mobileControllerPrefabName: ""
        );

        static string GlidingSystemWelcomeEditorKey => GlidingSystemSetup.welcomeEditorShowKey;


        [InitializeOnLoadMethod]
        public static void LoadGlidingSystem()
        {
            if (!string.IsNullOrEmpty(GlidingSystemWelcomeEditorKey) && !EditorPrefs.GetBool(GlidingSystemWelcomeEditorKey, false))
            {
                SessionState.SetBool(welcomeWindowOpenKey, false);
                EditorPrefs.SetBool(GlidingSystemWelcomeEditorKey, true);
                FSSystemsSetupEditorWindow.OnProjectLoad();
            }
        }
    }
}
#endif