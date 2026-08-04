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
        // Option row GameObjects, refreshed each menu open.
        private static GameObject previewUIBackgroundOptionObject;
        private static GameObject previewUIBackgroundOpacityOptionObject;

        [Choice(LabelLanguageId = "DisplayMode", TooltipLanguageId = "Tooltip_DisplayMode")]
        [OnChange(nameof(OnDisplayModeChanged))]
        public DisplayMode DisplayMode = DisplayMode.DisplayModeDefault;

        // Only shown in Preview mode.
        [Toggle(LabelLanguageId = "PreviewUIBackround", TooltipLanguageId = "Tooltip_PreviewUIBackround")]
        [OnChange(nameof(OnPreviewUIBackroundChanged))]
        [OnGameObjectCreated(nameof(OnBackgroundOptionCreated))]
        public bool PreviewUIBackround = true;

        // Only shown in Preview mode with background enabled.
        [Slider(0f, 1f, DefaultValue = 0.75f, Format = "{0:F2}", LabelLanguageId = "PreviewUIBackgroundOpacity", TooltipLanguageId = "Tooltip_PreviewUIBackgroundOpacity")]
        [OnGameObjectCreated(nameof(OnPreviewUIBackgroundOpacityOptionCreated))]
        public float PreviewUIBackgroundOpacity = 0.75f;

        private void OnBackgroundOptionCreated(GameObjectCreatedEventArgs e)
        {
            previewUIBackgroundOptionObject = e.Value;
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

            if (previewUIBackgroundOptionObject != null)
            {
                previewUIBackgroundOptionObject.SetActive(preview);
            }

            if (previewUIBackgroundOpacityOptionObject != null)
            {
                previewUIBackgroundOpacityOptionObject.SetActive(preview && PreviewUIBackround);
            }
        }
    }
}
