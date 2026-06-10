using System;
using System.IO;
using KCompiler.Cli;
using Xunit;

namespace KCompiler.Tests
{
    public sealed class NwnnsscompCliParserTests
    {
        [Fact]
        public void SplitCommandLine_PreservesQuotedTokens()
        {
            string[] args = NwnnsscompCliParser.SplitCommandLine("--debug --nwscript \"defs/custom nwscript.nss\"");

            Assert.Equal(new[] { "--debug", "--nwscript", "defs/custom nwscript.nss" }, args);
        }

        [Fact]
        public void Parse_ResolvesRelativePathsAgainstProvidedBaseDirectory()
        {
            string baseDirectory = Path.Combine(Path.GetTempPath(), "nwnnsscomp_parse_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(baseDirectory);

            try
            {
                NwnnsscompParseResult result = NwnnsscompCliParser.Parse(
                    new[] { "-c", "--nwscript", "defs/custom.nss", "script.nss", "-o", "out/script.ncs" },
                    baseDirectory,
                    null);

                Assert.True(result.Success);
                Assert.Equal(Path.GetFullPath(Path.Combine(baseDirectory, "script.nss")), result.SourcePath);
                Assert.Equal(Path.GetFullPath(Path.Combine(baseDirectory, "out/script.ncs")), result.OutputPath);
                Assert.Equal(Path.GetFullPath(Path.Combine(baseDirectory, "defs/custom.nss")), result.NwscriptPath);
            }
            finally
            {
                Directory.Delete(baseDirectory, true);
            }
        }
    }
}