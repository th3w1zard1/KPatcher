using System;
using Microsoft.Extensions.Logging;

namespace KCompiler.Diagnostics
{
    public static class ToolLogLevel
    {
        /// <summary>Default: Warning (errors + important warnings only). Override with KPATCHER_TOOL_LOG_LEVEL (Trace|Debug|Information|Warning|Error|Critical).</summary>
        public static LogLevel DefaultMinimumFromEnvironment()
        {
            string s = Environment.GetEnvironmentVariable("KPATCHER_TOOL_LOG_LEVEL");
            if (string.IsNullOrWhiteSpace(s))
            {
                return LogLevel.Warning;
            }

            return Enum.TryParse(s, true, out LogLevel level) ? level : LogLevel.Warning;
        }
    }
}
