using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using KPatcher.Core.Logger;
using KPatcher.Core.Namespaces;
using Xunit;
using PatcherUiCore = KPatcher.UI.Core;

namespace KPatcher.Core.Tests.UI
{
    public sealed class CoreNamespaceFallbackTests : IDisposable
    {
        private readonly string _modRoot;
        private readonly string _tslPatchDataPath;

        public CoreNamespaceFallbackTests()
        {
            _modRoot = Path.Combine(Path.GetTempPath(), "KPatcher_NamespaceFallback_" + Guid.NewGuid().ToString("N"));
            _tslPatchDataPath = Path.Combine(_modRoot, "tslpatchdata");
            Directory.CreateDirectory(_tslPatchDataPath);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_modRoot))
                {
                    Directory.Delete(_modRoot, true);
                }
            }
            catch
            {
            }
        }

        [Fact]
        public void LoadNamespaceConfig_WhenNamespaceFilesAreMissing_ShouldFallBackToDefaultChangesAndInfo()
        {
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "namespaces.ini"), @"[Namespaces]
Namespace1=Default
Namespace2=French

[Default]
IniName=changes.ini
InfoName=info.rtf
Name=Default

[French]
IniName=translation_french\missing.ini
InfoName=translation_french\missing.rtf
Name=French
");
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "changes.ini"), "[Settings]\nWindowCaption=Default Install\n");
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "info.rtf"), "{\\rtf1\\ansi Default namespace info}");

            var logger = new PatchLogger();
            List<PatcherNamespace> namespaces = PatcherUiCore.LoadMod(_modRoot, logger).Namespaces;

            PatcherUiCore.NamespaceInfo info = PatcherUiCore.LoadNamespaceConfig(_modRoot, namespaces, "French", diagnosticLogger: logger);

            info.ConfigReader.Config.WindowTitle.Should().Be("Default Install");
            info.InfoContent.Should().Be("{\\rtf1\\ansi Default namespace info}");
            info.IsRtf.Should().BeTrue();
        }
    }
}
