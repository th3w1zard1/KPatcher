namespace KPatcher.Core.Common
{
    /// <summary>
    /// Normalizes mod INI text so parsers (e.g. IniParser) see a clean first line.
    /// UTF-8 BOM and Unicode BOM decode to U+FEFF; some load paths leave that character in the string.
    /// </summary>
    public static class IniTextNormalizer
    {
        /// <summary>
        /// Removes leading Unicode BOM (U+FEFF), including repeated marks, from the start of the string.
        /// Does not trim whitespace; <see cref="KPatcher.Core.Reader.ConfigReader.PreprocessIniText"/> handles line structure.
        /// </summary>
        public static string StripLeadingBom(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            int i = 0;
            while (i < text.Length && text[i] == '\uFEFF')
            {
                i++;
            }

            return i == 0 ? text : text.Substring(i);
        }
    }
}
