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
        DisplayModePreview
    }

    [Menu("Storage Info")]
    public class StorageInfoOptions : ConfigFile
    {
        // Option row GameObjects, refreshed every time the options menu opens
        // (uGUI_OptionsPanel.AddTabs -> AddOptionsToPanel). Separate references are
        // needed so ApplyBackgroundVisibility can toggle BOTH rows live when the
        // DisplayMode choice changes while the menu is open.
        private static GameObject backgroundOptionObject;
        private static GameObject previewUIBackgroundOpacityOptionObject;

        [Choice(LabelLanguageId = "DisplayMode", TooltipLanguageId = "Tooltip_DisplayMode")]
        [OnChange(nameof(OnDisplayModeChanged))]
        public DisplayMode DisplayMode = DisplayMode.DisplayModeDefault;

        // Only shown in the options menu while DisplayMode == Preview. Nautilus has no
        // built-in conditional visibility, so visibility is driven manually: each row's
        // OnGameObjectCreated callback (fires every time the options menu opens and the
        // row is built) applies the initial state; OnDisplayModeChanged / OnPreviewUIBackroundChanged
        // re-apply it live while the menu is open.
        [Toggle(LabelLanguageId = "PreviewUIBackround", TooltipLanguageId = "Tooltip_PreviewUIBackround")]
        [OnChange(nameof(OnPreviewUIBackroundChanged))]
        [OnGameObjectCreated(nameof(OnBackgroundOptionCreated))]
        public bool PreviewUIBackround = true;

        // Only shown while DisplayMode == Preview AND PreviewUIBackround is enabled.
        [Slider(0f, 1f, DefaultValue = 0.75f, Format = "{0:F2}", LabelLanguageId = "PreviewUIBackgroundOpacity", TooltipLanguageId = "Tooltip_PreviewUIBackgroundOpacity")]
        [OnGameObjectCreated(nameof(OnPreviewUIBackgroundOpacityOptionCreated))]
        public float PreviewUIBackgroundOpacity = 0.75f;

        private void OnBackgroundOptionCreated(GameObjectCreatedEventArgs e)
        {
            backgroundOptionObject = e.Value;
            ApplyBackgroundVisibility();
        }

        private void OnPreviewUIBackroundChanged(object sender, ToggleChangedEventArgs e)
        {
            ApplyBackgroundVisibility();
        }

        private void OnPreviewUIBackgroundOpacityOptionCreated(GameObjectCreatedEventArgs e)
        {
            previewUIBackgroundOpacityOptionObject = e.Value;
            ApplyBackgroundVisibility();
        }

        private void OnDisplayModeChanged(object sender, ChoiceChangedEventArgs<DisplayMode> e)
        {
            ApplyBackgroundVisibility();
        }

        private void ApplyBackgroundVisibility()
        {
            // 1. Not in Preview mode: neither row is shown.
            // 2. Preview mode: PreviewUIBackround row shown.
            // 3. Preview mode + PreviewUIBackround enabled: opacity slider row also shown.
            bool preview = DisplayMode == DisplayMode.DisplayModePreview;

            if (backgroundOptionObject != null)
            {
                backgroundOptionObject.SetActive(preview);
            }

            if (previewUIBackgroundOpacityOptionObject != null)
            {
                previewUIBackgroundOpacityOptionObject.SetActive(preview && PreviewUIBackround);
            }
        }
    }
}
