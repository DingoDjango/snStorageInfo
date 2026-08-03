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

    public enum PreviewBackground
    {
        None,
        Enabled
    }

    [Menu("Storage Info")]
    public class StorageInfoOptions : ConfigFile
    {
        [Choice]
        public DisplayMode DisplayMode = DisplayMode.DisplayModeDefault;

        [Choice]
        public PreviewBackground Background = PreviewBackground.None;
    }
}
