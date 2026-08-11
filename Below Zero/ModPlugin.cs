using BepInEx;
using Nautilus.Handlers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StorageInfo
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("com.snmodding.nautilus")]
    public class ModPlugin : BaseUnityPlugin
    {
        public const string modGUID = "Dingo.SNBZ.StorageInfo";
        public const string modName = "Storage Info BZ";
        public const string modVersion = "3.0.8.3031";

        public static StorageInfoOptions options;
    
        private void Awake()
        {
            LanguageHandler.RegisterLocalizationFolder();
    
            options = OptionsPanelHandler.RegisterModOptions<StorageInfoOptions>();
    
            HarmonyPatches.InitializeHarmony();
        }

        private void OnDestroy()
        {
            StorageDetailUI.Cleanup();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            HarmonyPatches.ResetSceneState();
            StorageDetailUI.Cleanup();
        }

        public static void LogMessage(string message)
        {
            Debug.Log($"{modName} :: {message}");
        }
    }
}
