using System;
using System.IO;
using System.Text;

namespace KCompiler.Diagnostics
{
    public static class ToolExceptionFormatter
    {
        public static bool IncludeStackTraces =>
            string.Equals(Environment.GetEnvironmentVariable("KPATCHER_TOOL_LOG_STACK"), "1", StringComparison.Ordinal);

        public static string Format(Exception ex, bool? includeStack = null)
        {
            if (ex == null)
            {
                return string.Empty;
            }

            bool stack = includeStack ?? IncludeStackTraces;
            var sb = new StringBuilder();
            for (Exception e = ex; e != null; e = e.InnerException)
            {
                sb.Append(e.GetType().FullName).Append(": ").Append(e.Message);
                if (e is FileNotFoundException fnf && !string.IsNullOrEmpty(fnf.FileName))
                {
                    sb.Append(" FileName=").Append(ToolPathRedaction.FormatPath(fnf.FileName));
                }

                sb.AppendLine();
            }

            if (stack)
            {
                sb.AppendLine(ex.StackTrace);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
