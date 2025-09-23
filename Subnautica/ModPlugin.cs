using BepInEx;
using Nautilus.Handlers;
using UnityEngine;

namespace StorageInfo
{
    [BepInPlugin(modGUID, modName, modVersion)]
	[BepInDependency("com.snmodding.nautilus")]
	public class ModPlugin : BaseUnityPlugin
    {
        public const string modGUID = "Dingo.SN.StorageInfo";
        public const string modName = "Storage Info";
        public const string modVersion = "2.2.0";

        public static void LogMessage(string message)
        {
            Debug.Log($"{modName} :: ${message}");
		}

		private void Awake()
		{
			LanguageHandler.RegisterLocalizationFolder();

			HarmonyPatches.InitializeHarmony();
		}
	}
}
