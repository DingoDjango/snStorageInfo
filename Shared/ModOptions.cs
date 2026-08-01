using Nautilus.Handlers;
using Nautilus.Json;
using Nautilus.Options.Attributes;

namespace StorageInfo
{
    public enum DisplayMode
    {
        DisplayModeDefault,
        DisplayModeSlotsOnly,
        DisplayModeDetailedList
    }

    public enum AnchorPreset
    {
        TopLeft,
        TopCenter,
        TopRight,
        CenterLeft,
        Center,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
        Custom
    }

    [Menu("Storage Info")]
    public class StorageInfoOptions : ConfigFile
    {
        [Choice]
        public DisplayMode DisplayMode = DisplayMode.DisplayModeDefault;

        // UI Position Options
        [Choice]
        public AnchorPreset PanelAnchor = AnchorPreset.TopRight;

        [Slider(null, 0f, 1f)]
        public float CustomAnchorX = 0.75f;

        [Slider(null, 0f, 1f)]
        public float CustomAnchorY = 0.5f;

        [Slider(null, -500f, 500f)]
        public float OffsetX = 0f;

        [Slider(null, -500f, 500f)]
        public float OffsetY = 0f;

        [Toggle]
        public bool UseGamePanelStyle = true;

        [Slider(null, 0f, 1f)]
        public float PanelOpacity = 0.9f;

        [Slider(null, 0f, 1f)]
        public float GridAlpha = 0.25f;
    }
}
