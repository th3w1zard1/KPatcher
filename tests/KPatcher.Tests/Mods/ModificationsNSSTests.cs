using System;
using System.IO;
using System.Linq;
using System.Text;
using KPatcher.Core.Common;
using KPatcher.Core.Logger;
using KPatcher.Core.Memory;
using KPatcher.Core.Mods.NSS;
using Xunit;

namespace KPatcher.Core.Tests.Mods
{
    public sealed class ModificationsNSSTests : IDisposable
    {
        private readonly string _tempDir;

        public ModificationsNSSTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Fact]
        public void PatchResource_ScriptCompilerFlagsNwscript_UsesManagedCliSettings()
        {
            string nwscriptPath = Path.Combine(_tempDir, "custom.nss");
            File.WriteAllText(nwscriptPath, "void Helper();\n");

            var patch = new ModificationsNSS("test.nss", false)
            {
                TempScriptFolder = _tempDir,
                CompilerWorkingDirectory = _tempDir,
                ScriptCompilerFlags = "--nwscript \"custom.nss\""
            };
            byte[] source = Encoding.GetEncoding("windows-1252").GetBytes("void main() { Helper(); }\n");

            object result = patch.PatchResource(source, new PatcherMemory(), new PatchLogger(), Game.K1);

            byte[] compiled = Assert.IsType<byte[]>(result);
            Assert.NotEmpty(compiled);
        }

        [Fact]
        public void PatchResource_ImplicitSiblingFolderInclude_DoesNotResolveOutsideScriptDirOrRoot()
        {
            string scriptFolder = Path.Combine(_tempDir, "scripts", "main");
            string siblingFolder = Path.Combine(_tempDir, "scripts", "shared");
            Directory.CreateDirectory(scriptFolder);
            Directory.CreateDirectory(siblingFolder);
            File.WriteAllText(Path.Combine(siblingFolder, "helper.nss"), "void Helper() { PrintInteger(123); }\n");

            var patch = new ModificationsNSS("test.nss", false)
            {
                TempScriptFolder = _tempDir,
                SourceFolder = Path.Combine("scripts", "main"),
                CompilerWorkingDirectory = _tempDir
            };
            byte[] source = Encoding.GetEncoding("windows-1252").GetBytes("#include \"helper\"\nvoid main() { Helper(); }\n");
            var logger = new PatchLogger();

            object result = patch.PatchResource(source, new PatcherMemory(), logger, Game.K1);

            Assert.True(result is bool skipped && skipped);
            Assert.Contains(
                logger.Errors,
                log => log.Message.IndexOf("did not produce an NCS file", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.Contains(
                logger.VerboseLogs,
                log => log.Message.IndexOf("helper", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [Fact]
        public void PatchResource_DuplicateIncludeNames_PrefersScriptDirectoryOverRoot()
        {
            string scriptFolder = Path.Combine(_tempDir, "scripts", "main");
            Directory.CreateDirectory(scriptFolder);

            File.WriteAllText(
                Path.Combine(scriptFolder, "helper.nss"),
                "int HelperValue() { return 123; }\n");
            File.WriteAllText(
                Path.Combine(_tempDir, "helper.nss"),
                "string HelperValue() { return \"wrong\"; }\n");

            var patch = new ModificationsNSS("test.nss", false)
            {
                TempScriptFolder = _tempDir,
                SourceFolder = Path.Combine("scripts", "main"),
                CompilerWorkingDirectory = _tempDir
            };
            byte[] source = Encoding.GetEncoding("windows-1252").GetBytes(
                "#include \"helper\"\nvoid main() { int value = HelperValue(); PrintInteger(value); }\n");

            object result = patch.PatchResource(source, new PatcherMemory(), new PatchLogger(), Game.K1);

            byte[] compiled = Assert.IsType<byte[]>(result);
            Assert.NotEmpty(compiled);
        }

        [Fact]
        public void PatchResource_MissingInclude_ReturnsSkipAndLogsVerboseCompilerFeedback()
        {
            string scriptFolder = Path.Combine(_tempDir, "scripts", "main");
            Directory.CreateDirectory(scriptFolder);

            var patch = new ModificationsNSS("test.nss", false)
            {
                TempScriptFolder = _tempDir,
                SourceFolder = Path.Combine("scripts", "main"),
                CompilerWorkingDirectory = _tempDir
            };
            byte[] source = Encoding.GetEncoding("windows-1252").GetBytes(
                "#include \"missing_helper\"\nvoid main() { PrintInteger(1); }\n");
            var logger = new PatchLogger();

            object result = patch.PatchResource(source, new PatcherMemory(), logger, Game.K1);

            Assert.True(result is bool skipped && skipped);
            PatchLog[] verboseLogs = logger.VerboseLogs.ToArray();
            PatchLog[] errorLogs = logger.Errors.ToArray();

            Assert.Single(errorLogs);
            Assert.Contains(
                errorLogs,
                log => log.Message.IndexOf("did not produce an NCS file", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.DoesNotContain(
                errorLogs,
                log => log.Message.IndexOf("Could not find included script", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.Contains(
                verboseLogs,
                log => log.Message.IndexOf("Could not find included script", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.Contains(
                verboseLogs,
                log => log.Message.IndexOf("Searched in", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.Contains(
                verboseLogs,
                log => log.Message.IndexOf("Also checked", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.All(
                verboseLogs,
                log => Assert.DoesNotContain("\n", log.Message));

            int firstVerboseIndex = logger.AllLogs.ToList().FindIndex(log => log.LogType == LogType.Verbose);
            int firstErrorIndex = logger.AllLogs.ToList().FindIndex(log => log.LogType == LogType.Error);
            Assert.True(firstVerboseIndex >= 0 && firstErrorIndex > firstVerboseIndex);
        }

        [Fact]
        public void PatchResource_EntryPointError_ReturnsSkipAndLogsVerboseCompilerFeedback()
        {
            string scriptFolder = Path.Combine(_tempDir, "scripts", "main");
            Directory.CreateDirectory(scriptFolder);

            var patch = new ModificationsNSS("test.nss", false)
            {
                TempScriptFolder = _tempDir,
                SourceFolder = Path.Combine("scripts", "main"),
                CompilerWorkingDirectory = _tempDir
            };
            byte[] source = Encoding.GetEncoding("windows-1252").GetBytes("void main();\n");
            var logger = new PatchLogger();

            object result = patch.PatchResource(source, new PatcherMemory(), logger, Game.K1);

            Assert.True(result is bool skipped && skipped);
            PatchLog[] verboseLogs = logger.VerboseLogs.ToArray();
            PatchLog[] errorLogs = logger.Errors.ToArray();

            Assert.Contains(
                verboseLogs,
                log => log.Message.IndexOf("entry instruction", StringComparison.OrdinalIgnoreCase) >= 0
                    || log.Message.IndexOf("no entry point", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.Single(errorLogs);
            Assert.Contains(
                errorLogs,
                log => log.Message.IndexOf("did not produce an NCS file", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.DoesNotContain(
                errorLogs,
                log => log.Message.IndexOf("entry instruction", StringComparison.OrdinalIgnoreCase) >= 0
                    || log.Message.IndexOf("no entry point", StringComparison.OrdinalIgnoreCase) >= 0);

            int firstVerboseIndex = logger.AllLogs.ToList().FindIndex(log => log.LogType == LogType.Verbose);
            int firstErrorIndex = logger.AllLogs.ToList().FindIndex(log => log.LogType == LogType.Error);
            Assert.True(firstVerboseIndex >= 0 && firstErrorIndex > firstVerboseIndex);
        }
    }
}