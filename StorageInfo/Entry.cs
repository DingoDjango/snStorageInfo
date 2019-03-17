using System;
using UnityEngine;

namespace StorageInfo
{
    public class Entry
    {
        public static void Initialize()
        {
            try
            {
                HarmonyPatches.InitializeHarmony();
            }

            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
                Debug.Log("[StorageInfo] :: Error during mod initialization!");
            }
        }
    }
}
