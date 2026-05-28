using System;
using System.IO;
using FluentAssertions;
using IniParser.Model;
using IniParser.Parser;
using KPatcher.Core.Config;
using KPatcher.Core.Reader;
using Xunit;

namespace KPatcher.Core.Tests.Reader
{
    public sealed class ConfigReaderCompileListTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _modPath;
        private readonly IniDataParser _parser;

        public ConfigReaderCompileListTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _modPath = Path.Combine(_tempDir, "tslpatchdata");
            Directory.CreateDirectory(_modPath);

            _parser = new IniDataParser();
            _parser.Configuration.AllowDuplicateKeys = true;
            _parser.Configuration.AllowDuplicateSections = true;
            _parser.Configuration.CaseInsensitive = false;
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Fact]
        public void CompileList_DefaultSourceFolder_IsIgnored()
        {
            string iniText = @"
[CompileList]
!DefaultSourceFolder=alt
File0=test.nss
";
            IniData ini = _parser.Parse(iniText);
            var reader = new ConfigReader(ini, _tempDir, null, _modPath);

            PatcherConfig result = reader.Load(new PatcherConfig());

            result.PatchesNSS.Should().ContainSingle();
            result.PatchesNSS[0].SourceFolder.Should().Be(".");
        }

        [Fact]
        public void CompileList_PropagatesScriptCompilerFlags_FromSettings()
        {
            const string iniText = @"
[Settings]
ScriptCompilerFlags=--debug --nwscript ""defs/custom nwscript.nss""

[CompileList]
File0=test.nss
";
            IniData ini = _parser.Parse(iniText);
            var reader = new ConfigReader(ini, _tempDir, null, _modPath);

            PatcherConfig result = reader.Load(new PatcherConfig());

            result.ScriptCompilerFlags.Should().Be("--debug --nwscript \"defs/custom nwscript.nss\"");
            result.PatchesNSS.Should().ContainSingle();
            result.PatchesNSS[0].ScriptCompilerFlags.Should().Be(result.ScriptCompilerFlags);
            result.PatchesNSS[0].CompilerWorkingDirectory.Should().Be(Path.GetFullPath(_modPath));
        }
    }
}