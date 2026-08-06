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
        DisplayModeDisabled
    }

    [Menu("Storage Info")]
    public class StorageInfoOptions : ConfigFile
    {
        // Option row GameObjects, refreshed each menu open.
        private static GameObject previewUIBackgroundOptionObject;
        private static GameObject previewUIBackgroundOpacityOptionObject;
        private static GameObject previewUIAnchorXOptionObject;
        private static GameObject previewUIAnchorYOptionObject;

        [Choice(LabelLanguageId = "DisplayMode", TooltipLanguageId = "Tooltip_DisplayMode")]
        public DisplayMode DisplayMode = DisplayMode.DisplayModeDefault;

        [Toggle(LabelLanguageId = "PreviewUI", TooltipLanguageId = "Tooltip_PreviewUI")]
        [OnChange(nameof(OnPreviewUIChanged))]
        public bool PreviewUI = false;

        // Only shown when Preview UI toggle is enabled.
        [Slider(0f, 1f, DefaultValue = StorageDetailUI.PanelAnchorX, Format = "{0:F2}", LabelLanguageId = "PreviewUIAnchorX", TooltipLanguageId = "Tooltip_PreviewUIAnchorX")]
        [OnChange(nameof(OnPreviewUIAnchorXChanged))]
        [OnGameObjectCreated(nameof(OnPreviewUIAnchorXOptionCreated))]
        public float PreviewUIAnchorX = StorageDetailUI.PanelAnchorX;

        // Only shown in Preview mode.
        [Slider(0f, 1f, DefaultValue = StorageDetailUI.PanelAnchorY, Format = "{0:F2}", LabelLanguageId = "PreviewUIAnchorY", TooltipLanguageId = "Tooltip_PreviewUIAnchorY")]
        [OnChange(nameof(OnPreviewUIAnchorYChanged))]
        [OnGameObjectCreated(nameof(OnPreviewUIAnchorYOptionCreated))]
        public float PreviewUIAnchorY = StorageDetailUI.PanelAnchorY;

        // Only shown in Preview mode.
        [Toggle(LabelLanguageId = "PreviewUIBackround", TooltipLanguageId = "Tooltip_PreviewUIBackround")]
        [OnChange(nameof(OnPreviewUIBackroundChanged))]
        [OnGameObjectCreated(nameof(OnBackgroundOptionCreated))]
        public bool PreviewUIBackround = true;

        // Only shown in Preview mode with background enabled.
        [Slider(0f, 1f, DefaultValue = 0.75f, Format = "{0:F2}", LabelLanguageId = "PreviewUIBackgroundOpacity", TooltipLanguageId = "Tooltip_PreviewUIBackgroundOpacity")]
        [OnChange(nameof(OnPreviewUIBackgroundOpacityChanged))]
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
            StorageDetailUI.RefreshAppearance();
        }

        private void OnPreviewUIBackgroundOpacityChanged(object sender, SliderChangedEventArgs e)
        {
            StorageDetailUI.RefreshAppearance();
        }

        private void OnPreviewUIAnchorXChanged(object sender, SliderChangedEventArgs e)
        {
            StorageDetailUI.RefreshAppearance();
        }

        private void OnPreviewUIAnchorYChanged(object sender, SliderChangedEventArgs e)
        {
            StorageDetailUI.RefreshAppearance();
        }

        private void OnPreviewUIAnchorXOptionCreated(GameObjectCreatedEventArgs e)
        {
            previewUIAnchorXOptionObject = e.Value;
            ApplyBackgroundVisibility();
        }

        private void OnPreviewUIAnchorYOptionCreated(GameObjectCreatedEventArgs e)
        {
            previewUIAnchorYOptionObject = e.Value;
            ApplyBackgroundVisibility();
        }

        private void OnPreviewUIBackgroundOpacityOptionCreated(GameObjectCreatedEventArgs e)
        {
            previewUIBackgroundOpacityOptionObject = e.Value;
            ApplyBackgroundVisibility();
        }

        private void OnPreviewUIChanged(object sender, ToggleChangedEventArgs e)
        {
            ApplyBackgroundVisibility();
            StorageDetailUI.RefreshAppearance();

            // Immediate hide when the toggle is turned off (avoids waiting for next hover frame).
            if (!e.Value)
            {
                StorageDetailUI.Hide();
            }
        }

        private void ApplyBackgroundVisibility()
        {
            // 1. Preview UI off: neither row is shown.
            // 2. Preview UI on: PreviewUIBackround row shown.
            // 3. Preview UI on + PreviewUIBackround enabled: opacity slider row also shown.
            bool preview = PreviewUI;

            if (previewUIBackgroundOptionObject != null)
            {
                previewUIBackgroundOptionObject.SetActive(preview);
            }

            if (previewUIBackgroundOpacityOptionObject != null)
            {
                previewUIBackgroundOpacityOptionObject.SetActive(preview && PreviewUIBackround);
            }

            if (previewUIAnchorXOptionObject != null)
            {
                previewUIAnchorXOptionObject.SetActive(preview);
            }

            if (previewUIAnchorYOptionObject != null)
            {
                previewUIAnchorYOptionObject.SetActive(preview);
            }
        }
    }
}
