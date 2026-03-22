using System;
using System.IO;

namespace KCompiler.Diagnostics
{
    /// <summary>Path display for tool logs: basename by default, full path when KPATCHER_LOG_FULL_PATHS=1.</summary>
    public static class ToolPathRedaction
    {
        public static bool UseFullPaths =>
            string.Equals(Environment.GetEnvironmentVariable("KPATCHER_LOG_FULL_PATHS"), "1", StringComparison.Ordinal);

        public static string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path ?? string.Empty;
            }

            if (UseFullPaths)
            {
                return path;
            }

            try
            {
                return Path.GetFileName(path) ?? path;
            }
            catch
            {
                return "<path>";
            }
        }
    }
}
