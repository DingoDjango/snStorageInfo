using System;

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

        internal static string FormatTranslate(this string source, params object[] args)
        {
            string basic = source.Translate();

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(basic, args);
                }

                catch (Exception ex)
                {
                    ModPlugin.LogMessage(ex.ToString());
                    ModPlugin.LogMessage($"Failed to format '{source}'");
                }
            }

            return basic;
        }

        internal static string TryFormatTranslate(this string source, params object[] args)
        {
            if (!Language.main.TryGet(source, out string basic))
            {
                return null;
            }

            if (args == null || args.Length == 0)
            {
                return basic;
            }

            try
            {
                return string.Format(basic, args);
            }

            catch (Exception ex)
            {
                ModPlugin.LogMessage(ex.ToString());
                ModPlugin.LogMessage($"Failed to format '{source}'");
                return null;
            }
        }
    }
}
