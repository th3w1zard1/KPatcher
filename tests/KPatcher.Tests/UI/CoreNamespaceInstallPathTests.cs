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
    public sealed class CoreNamespaceInstallPathTests : IDisposable
    {
        private readonly string _modRoot;
        private readonly string _tslPatchDataPath;

        public CoreNamespaceInstallPathTests()
        {
            _modRoot = Path.Combine(Path.GetTempPath(), "KPatcher_InstallPath_" + Guid.NewGuid().ToString("N"));
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
        public void ResolveInstallPaths_WhenNamespaceFilesAreMissing_ShouldFallBackToDefaultChangesIni()
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
            string defaultChanges = Path.Combine(_tslPatchDataPath, "changes.ini");
            File.WriteAllText(defaultChanges, "[Settings]\nWindowCaption=Default Install\n");

            var logger = new PatchLogger();
            List<PatcherNamespace> namespaces = PatcherUiCore.LoadMod(_modRoot, logger).Namespaces;

            PatcherUiCore.InstallPathResolution paths = PatcherUiCore.ResolveInstallPaths(
                _modRoot,
                namespaces,
                "French",
                logger);

            paths.IniFilePath.Should().Be(defaultChanges);
            paths.TslPatchDataPath.Should().Be(_tslPatchDataPath);
            paths.NamespaceModPath.Should().Be(_tslPatchDataPath);
        }
    }
}
