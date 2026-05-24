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
                // Get the filename, but handle both forward and backward slashes
                // (important for cross-platform path handling, e.g., Windows paths on Linux)
                int lastSlashIndex = Math.Max(
                    path.LastIndexOf('/'),
                    path.LastIndexOf('\\')
                );
                
                if (lastSlashIndex >= 0 && lastSlashIndex < path.Length - 1)
                {
                    return path.Substring(lastSlashIndex + 1);
                }
                
                return path;
            }
            catch
            {
                return "<path>";
            }
        }
    }
}
