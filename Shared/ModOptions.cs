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

    [Menu("Storage Info")]
    public class StorageInfoOptions : ConfigFile
    {
        // [Choice("DisplayMode", "DisplayModeDefault", "DisplayModeSlotsOnly", "DisplayModeDetailedList")]
        [Choice]
        public DisplayMode DisplayMode = DisplayMode.DisplayModeDefault;
    }
}
