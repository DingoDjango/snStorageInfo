using Nautilus.Handlers;
using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using UnityEngine;

namespace StorageInfo
{
    public enum DisplayMode
    {
        DisplayModeDefault,
        DisplayModeSlotsOnly,
        DisplayModeDetailedList
    }

    [Menu("Storage Info")]
    public class StorageInfoOptions : ConfigFile
    {
        // The Background option row GameObject. Recreated every time the options menu
        // opens (uGUI_OptionsPanel.AddTabs -> AddOptionsToPanel), so the reference is
        // refreshed in OnBackgroundOptionCreated.
        private static GameObject backgroundOptionObject;

        [Choice]
        [OnChange(nameof(OnDisplayModeChanged))]
        public DisplayMode DisplayMode = DisplayMode.DisplayModeDefault;

        // Only shown in the options menu while DisplayMode == DetailedList. Nautilus
        // has no built-in conditional visibility, so visibility is driven manually:
        // OnBackgroundOptionCreated (fires each time the options menu opens and the
        // option row is built) applies the initial state; OnDisplayModeChanged toggles
        // it live while the menu is open.
        [Toggle]
        [OnGameObjectCreated(nameof(OnBackgroundOptionCreated))]
        public bool Background = true;

        private void OnBackgroundOptionCreated(GameObjectCreatedEventArgs e)
        {
            backgroundOptionObject = e.Value;
            ApplyBackgroundVisibility();
        }

        private void OnDisplayModeChanged(object sender, ChoiceChangedEventArgs<DisplayMode> e)
        {
            ApplyBackgroundVisibility();
        }

        private void ApplyBackgroundVisibility()
        {
            if (backgroundOptionObject != null)
            {
                backgroundOptionObject.SetActive(DisplayMode == DisplayMode.DisplayModeDetailedList);
            }
        }
    }
}
