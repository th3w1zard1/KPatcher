using System;
using System.IO;
using KCompiler.Diagnostics;
using Xunit;

namespace KCompiler.Tests
{
    public sealed class ToolDiagnosticsTests
    {
        [Fact]
        public void ToolPathRedaction_UsesBasename_WhenFullPathsNotEnabled()
        {
            string prev = Environment.GetEnvironmentVariable("KPATCHER_LOG_FULL_PATHS");
            try
            {
                Environment.SetEnvironmentVariable("KPATCHER_LOG_FULL_PATHS", null);
                string p = Path.Combine("C:", "Users", "x", "mod", "foo.nss");
                Assert.Equal("foo.nss", ToolPathRedaction.FormatPath(p));
            }
            finally
            {
                Environment.SetEnvironmentVariable("KPATCHER_LOG_FULL_PATHS", prev);
            }
        }

        [Fact]
        public void ToolExceptionFormatter_IncludesFileName_ForFileNotFoundException()
        {
            var ex = new FileNotFoundException("missing", "C:\\secret\\gone.nss");
            string prev = Environment.GetEnvironmentVariable("KPATCHER_LOG_FULL_PATHS");
            try
            {
                Environment.SetEnvironmentVariable("KPATCHER_LOG_FULL_PATHS", null);
                string s = ToolExceptionFormatter.Format(ex, includeStack: false);
                Assert.Contains("gone.nss", s);
                Assert.DoesNotContain("secret", s);
            }
            finally
            {
                Environment.SetEnvironmentVariable("KPATCHER_LOG_FULL_PATHS", prev);
            }
        }

        [Fact]
        public void ToolCliStderr_IncludesTypeAndCorrelation_FromEnvironment()
        {
            string prev = Environment.GetEnvironmentVariable(ToolCorrelation.EnvironmentVariableName);
            try
            {
                Environment.SetEnvironmentVariable(ToolCorrelation.EnvironmentVariableName, "abc123");
                var ex = new InvalidOperationException("nope");
                string line = ToolCliStderr.FormatExceptionOneLiner(ex);
                Assert.Contains("InvalidOperationException", line);
                Assert.Contains("nope", line);
                Assert.Contains("CorrelationId=abc123", line);
            }
            finally
            {
                Environment.SetEnvironmentVariable(ToolCorrelation.EnvironmentVariableName, prev);
            }
        }
    }
}
