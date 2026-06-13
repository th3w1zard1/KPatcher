// Copyright (c) 2025 KEditChanges contributors / KPatcher
// UST_IniFile escape parity (ChangeEdit / TSLPatcher changes.ini).

namespace KEditChanges.Ini
{
    /// <summary>
    /// Encode/decode TSLPatcher-style INI escapes used by ChangeEdit (UST_IniFile).
    /// </summary>
    public static class TstIniEscapes
    {
        public static string Decode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Replace("<#LF#>", "\n").Replace("<#CR#>", "\r");
        }

        public static string Encode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Replace("\r", "<#CR#>").Replace("\n", "<#LF#>");
        }
    }
}
