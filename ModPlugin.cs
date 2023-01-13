using BepInEx;
using UnityEngine;

namespace StorageInfo
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class ModPlugin : BaseUnityPlugin
    {
        private const string modGUID = "Dingo.SN.StorageInfo";
        internal const string modName = "Storage Info";
        private const string modVersion = "2.0.0";

        internal static void LogMessage(string message)
        {
            Debug.Log($"{modName} :: " + message);
        }

        public void Start()
        {
            HarmonyPatches.InitializeHarmony();
        }
    }
}
