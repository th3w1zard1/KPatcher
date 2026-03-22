using System;

namespace KCompiler.Diagnostics
{
    /// <summary>Correlation id propagated from umbrella CLIs via environment.</summary>
    public static class ToolCorrelation
    {
        public const string EnvironmentVariableName = "KPATCHER_CORRELATION_ID";

        public static string ReadOptional()
        {
            return Environment.GetEnvironmentVariable(EnvironmentVariableName);
        }
    }
}
