using System.IO;
using KPatcher;
using Xunit;

namespace KPatcher.Tests
{
    public sealed class HoloPatcherCliTests
    {
        [Fact]
        public void ParseArgs_SetsHelp_ForLongHelp()
        {
            var a = HoloPatcherCli.ParseArgs(new[] { "--help" });
            Assert.True(a.Help);
        }

        [Fact]
        public void ParseArgs_SetsHelp_ForShortHelp()
        {
            var a = HoloPatcherCli.ParseArgs(new[] { "-h" });
            Assert.True(a.Help);
        }

        [Fact]
        public void ParseArgs_HelpTrue_AlongsideOtherFlags()
        {
            var a = HoloPatcherCli.ParseArgs(new[] { "--install", "--help" });
            Assert.True(a.Help);
            Assert.True(a.Install);
        }

        [Fact]
        public void WriteHelp_ContainsCoreOptions()
        {
            var sw = new StringWriter();
            HoloPatcherCli.WriteHelp(sw);
            string text = sw.ToString();
            Assert.Contains("--install", text);
            Assert.Contains("--tslpatchdata", text);
            Assert.Contains("--game-dir", text);
            Assert.Contains("--help", text);
        }
    }
}
