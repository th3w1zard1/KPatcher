using System;

namespace KCompiler.Diagnostics
{
    /// <summary>User-visible stderr lines that align with structured logs (type + correlation).</summary>
    public static class ToolCliStderr
    {
        public static string FormatExceptionOneLiner(Exception ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }

            string cid = ToolCorrelation.ReadOptional();
            string c = string.IsNullOrEmpty(cid) ? "-" : cid;
            return ex.GetType().Name + ": " + ex.Message + " CorrelationId=" + c;
        }
    }
}
