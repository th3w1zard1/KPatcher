using System;
using System.IO;
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
    }
}