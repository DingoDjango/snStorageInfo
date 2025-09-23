using System;
using System.Collections.Generic;
using System.IO;
using LitJson;

namespace StorageInfo
{
    internal static class Translation
    {
        internal static string Translate(this string source)
        {
            if (Language.main.TryGet(source, out string translated))
            {
                return translated;
            }

            ModPlugin.LogMessage($"Could not find translated string for `{source}`");

            return source;
        }

        internal static string FormatTranslate(this string source, string arg0)
        {
            string basic = source.Translate();

            if (!string.IsNullOrEmpty(arg0))
            {
                try
                {
                    return string.Format(basic, arg0);
                }

                catch (Exception ex)
                {
                    ModPlugin.LogMessage(ex.ToString());
                    ModPlugin.LogMessage($"Failed to format '{source}' with arg0 `{arg0}'");
                }
            }

            return basic;
        }
    }
}
