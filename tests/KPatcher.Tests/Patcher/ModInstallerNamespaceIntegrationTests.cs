using System.IO;
using FluentAssertions;
using KPatcher.Core.Logger;
using KPatcher.Core.Patcher;
using KPatcher.Core.Tests.Patcher.Support;
using Xunit;

namespace KPatcher.Core.Tests.Patcher
{
    public sealed class ModInstallerNamespaceIntegrationTests
    {
        [Fact]
        public void Install_NamespaceDataPathSubfolder_ResolvesAssetsFromSubfolderIni()
        {
            using (var env = new ModInstallerIntegrationEnvironment("KPatcher_Namespace_"))
            {
                string nsFolder = Path.Combine(env.TslPatchDataPath, "french");
                Directory.CreateDirectory(nsFolder);
                File.WriteAllText(Path.Combine(nsFolder, "marker.txt"), "from_namespace");

                env.WriteChangesIni(@"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
folder0=Override

[folder0]
File0=marker.txt
", "french/changes.ini");

                string iniPath = Path.Combine(env.TslPatchDataPath, "french", "changes.ini");
                var installer = new ModInstaller(env.ModRoot, env.GameRoot, iniPath, new PatchLogger());
                installer.TslPatchDataPath.Should().Be(Path.Combine(env.TslPatchDataPath, "french"));

                installer.Install();

                InstallAssertionLadder.AssertTextEqualL1(
                    Path.Combine(env.OverridePath, "marker.txt"),
                    "from_namespace");
            }
        }

        [Fact]
        public void Install_AlternateIniName_InRootTslpatchdata_AppliesPatches()
        {
            using (var env = new ModInstallerIntegrationEnvironment("KPatcher_AltIni_"))
            {
                File.WriteAllText(Path.Combine(env.TslPatchDataPath, "optional.txt"), "optional_install");
                env.WriteChangesIni("[Settings]\nLogLevel=3\n", "changes.ini");
                env.WriteChangesIni(@"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
folder0=Override

[folder0]
File0=optional.txt
", "optional.ini");

                string iniPath = Path.Combine(env.TslPatchDataPath, "optional.ini");
                var installer = new ModInstaller(env.ModRoot, env.GameRoot, iniPath, new PatchLogger());
                installer.Install();

                InstallAssertionLadder.AssertTextEqualL1(
                    Path.Combine(env.OverridePath, "optional.txt"),
                    "optional_install");
            }
        }
    }
}
