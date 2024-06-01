using BepInEx;
using UnityEngine;

namespace StorageInfo
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class ModPlugin : BaseUnityPlugin
    {
        public const string modGUID = "Dingo.SNBZ.StorageInfo";
        public const string modName = "Storage Info BZ";
        public const string modVersion = "2.1.1";

        public static void LogMessage(string message)
        {
            Debug.Log($"{modName} :: ${message}");
        }

        public void Start()
        {
            HarmonyPatches.InitializeHarmony();
        }
    }
}
