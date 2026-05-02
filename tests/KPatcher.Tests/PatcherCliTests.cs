using System.IO;
using KPatcher;
using Xunit;

namespace KPatcher.Tests
{
    public sealed class KPatcherCLITests
    {
        [Fact]
        public void ParseArgs_SetsHelp_ForLongHelp()
        {
            var a = KPatcherCLI.ParseArgs(new[] { "--help" });
            Assert.True(a.Help);
        }

        [Fact]
        public void ParseArgs_SetsHelp_ForShortHelp()
        {
            var a = KPatcherCLI.ParseArgs(new[] { "-h" });
            Assert.True(a.Help);
        }

        [Fact]
        public void ParseArgs_HelpTrue_AlongsideOtherFlags()
        {
            var a = KPatcherCLI.ParseArgs(new[] { "--install", "--help" });
            Assert.True(a.Help);
            Assert.True(a.Install);
        }

        [Fact]
        public void WriteHelp_ContainsCoreOptions()
        {
            var sw = new StringWriter();
            KPatcherCLI.WriteHelp(sw);
            string text = sw.ToString();
            Assert.Contains("--install", text);
            Assert.Contains("--tslpatchdata", text);
            Assert.Contains("--game-dir", text);
            Assert.Contains("--help", text);
        }

        [Fact]
        public void HasCliWorkIndicators_False_ForConsoleOnly()
        {
            var a = KPatcherCLI.ParseArgs(new[] { "--console" });
            Assert.False(KPatcherCLI.HasCliWorkIndicators(a));
        }

        [Fact]
        public void HasCliWorkIndicators_True_WithInstall()
        {
            var a = KPatcherCLI.ParseArgs(new[] { "--console", "--install" });
            Assert.True(KPatcherCLI.HasCliWorkIndicators(a));
        }

        [Fact]
        public void HasCliWorkIndicators_True_WithPositionalPaths()
        {
            var a = KPatcherCLI.ParseArgs(new[] { "C:\\game", "C:\\mod" });
            Assert.True(KPatcherCLI.HasCliWorkIndicators(a));
        }
    }
}
