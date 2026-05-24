using System;
using System.IO;
using KPatcher.UI.Parity;
using Xunit;

namespace KPatcher.Tests
{
    public sealed class ProgramCliExecutionTests
    {
        [Fact]
        public void RunCli_ListNamespaces_PrintsNamespacesWithoutGameDirectory()
        {
            using (var mod = new TemporaryModDirectory())
            {
                mod.CreateNamespacedMod();

                var stdout = new StringWriter();
                var stderr = new StringWriter();

                int exitCode = Program.RunCli(
                    KPatcherCLI.ParseArgs(new[] { "--list-namespaces", "--tslpatchdata", mod.ModRootPath }),
                    stdout,
                    stderr);

                Assert.Equal(0, exitCode);
                Assert.Contains("0: Default", stdout.ToString());
                Assert.Contains("1: Optional", stdout.ToString());
                Assert.Equal(string.Empty, stderr.ToString());
            }
        }

        [Fact]
        public void RunCli_DryRun_PrintsConfigurationSummaryWithoutWritingYaml()
        {
            using (var mod = new TemporaryModDirectory())
            {
                mod.CreateNamespacedMod();

                var stdout = new StringWriter();
                var stderr = new StringWriter();

                int exitCode = Program.RunCli(
                    KPatcherCLI.ParseArgs(new[] { "--dry-run", "--tslpatchdata", mod.ModRootPath, "--namespace-option-index", "1" }),
                    stdout,
                    stderr);

                Assert.Equal(0, exitCode);
                Assert.Contains("CONFIGURATION SUMMARY", stdout.ToString());
                Assert.Contains("optional.ini", stdout.ToString());
                Assert.False(File.Exists(Path.Combine(mod.TslPatchDataPath, "optional.yaml")));
                Assert.Equal(string.Empty, stderr.ToString());
            }
        }

        [Fact]
        public void RunCli_DryRun_ChangesOnlyMod_UsesDefaultNamespaceWithoutWritingYaml()
        {
            using (var mod = new TemporaryModDirectory())
            {
                mod.CreateChangesOnlyMod();

                var stdout = new StringWriter();
                var stderr = new StringWriter();

                int exitCode = Program.RunCli(
                    KPatcherCLI.ParseArgs(new[] { "--dry-run", "--tslpatchdata", mod.ModRootPath }),
                    stdout,
                    stderr);

                Assert.Equal(0, exitCode);
                Assert.Contains("changes.ini", stdout.ToString());
                Assert.False(File.Exists(Path.Combine(mod.TslPatchDataPath, "changes.yaml")));
                Assert.Equal(string.Empty, stderr.ToString());
            }
        }

        [Fact]
        public void RunCli_DryRun_WithNegativeNamespaceIndex_ReturnsOutOfRangeError()
        {
            using (var mod = new TemporaryModDirectory())
            {
                mod.CreateNamespacedMod();

                var stdout = new StringWriter();
                var stderr = new StringWriter();

                int exitCode = Program.RunCli(
                    KPatcherCLI.ParseArgs(new[] { "--dry-run", "--tslpatchdata", mod.ModRootPath, "--namespace-option-index", "-1" }),
                    stdout,
                    stderr);

                Assert.Equal(4, exitCode);
                Assert.Contains("Namespace index -1 out of range", stderr.ToString());
            }
        }

        [Fact]
        public void RunCli_DryRun_MissingNamespaceConfig_ReturnsCliError()
        {
            using (var mod = new TemporaryModDirectory())
            {
                mod.CreateNamespacedMod(includeOptionalIni: false);

                var stdout = new StringWriter();
                var stderr = new StringWriter();

                int exitCode = Program.RunCli(
                    KPatcherCLI.ParseArgs(new[] { "--dry-run", "--tslpatchdata", mod.ModRootPath, "--namespace-option-index", "1" }),
                    stdout,
                    stderr);

                Assert.Equal(7, exitCode);
                Assert.Contains("FileNotFoundException", stderr.ToString());
                Assert.Contains("optional.ini", stderr.ToString());
            }
        }

        [Fact]
        public void RunCli_ParityReport_PrintsSharedLedgerWithoutRequiringPaths()
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.RunCli(
                KPatcherCLI.ParseArgs(new[] { "--parity-report" }),
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.Equal(ParityLedger.BuildReport(), stdout.ToString().TrimEnd());
            Assert.Equal(string.Empty, stderr.ToString());
        }

        [Fact]
        public void RunCli_InstallWithoutGameDirectory_ReturnsNumberOfArgs()
        {
            using (var mod = new TemporaryModDirectory())
            {
                mod.CreateNamespacedMod();

                var stdout = new StringWriter();
                var stderr = new StringWriter();

                int exitCode = Program.RunCli(
                    KPatcherCLI.ParseArgs(new[] { "--install", "--tslpatchdata", mod.ModRootPath }),
                    stdout,
                    stderr);

                Assert.Equal(2, exitCode);
                Assert.Contains("No game directory", stderr.ToString());
            }
        }

        [Fact]
        public void RunCli_MultipleOperations_ReturnsNumberOfArgs()
        {
            using (var mod = new TemporaryModDirectory())
            {
                mod.CreateNamespacedMod();

                var stdout = new StringWriter();
                var stderr = new StringWriter();

                int exitCode = Program.RunCli(
                    KPatcherCLI.ParseArgs(new[] { "--install", "--dry-run", "--tslpatchdata", mod.ModRootPath }),
                    stdout,
                    stderr);

                Assert.Equal(2, exitCode);
                Assert.Equal(string.Empty, stdout.ToString());
                Assert.Contains("Cannot run more than one CLI operation", stderr.ToString());
            }
        }

        [Fact]
        public void RunCli_NoOperationSpecified_ReturnsNumberOfArgs()
        {
            using (var mod = new TemporaryModDirectory())
            {
                mod.CreateNamespacedMod();

                var stdout = new StringWriter();
                var stderr = new StringWriter();

                int exitCode = Program.RunCli(
                    KPatcherCLI.ParseArgs(new[] { "--tslpatchdata", mod.ModRootPath }),
                    stdout,
                    stderr);

                Assert.Equal(2, exitCode);
                Assert.Equal(string.Empty, stdout.ToString());
                Assert.Contains("Must specify one CLI operation", stderr.ToString());
            }
        }

        [Fact]
        public void RunCli_Install_PrintsInstallRecordPath()
        {
            using (var mod = new TemporaryCliInstallDirectory())
            {
                mod.CreateInstallableMod();

                var stdout = new StringWriter();
                var stderr = new StringWriter();

                int exitCode = Program.RunCli(
                    KPatcherCLI.ParseArgs(new[] { "--install", "--tslpatchdata", mod.ModRootPath, "--game-dir", mod.GameRootPath }),
                    stdout,
                    stderr);

                Assert.Equal(0, exitCode);
                Assert.Contains("Install record written to", stdout.ToString());
                Assert.True(File.Exists(Path.Combine(mod.TslPatchDataPath, "installrecord.txt")));
                Assert.Equal(string.Empty, stderr.ToString());
            }
        }

        private sealed class TemporaryModDirectory : IDisposable
        {
            public string ModRootPath { get; }
            public string TslPatchDataPath { get; }

            public TemporaryModDirectory()
            {
                ModRootPath = Path.Combine(Path.GetTempPath(), "KPatcher_CliRun_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                TslPatchDataPath = Path.Combine(ModRootPath, "tslpatchdata");
                Directory.CreateDirectory(TslPatchDataPath);
            }

            public void CreateNamespacedMod(bool includeOptionalIni = true)
            {
                File.WriteAllText(Path.Combine(TslPatchDataPath, "namespaces.ini"), @"[Namespaces]
Namespace1=Default
Namespace2=Optional

[Default]
IniName=changes.ini
InfoName=info.rtf
Name=Default
Description=Default install

[Optional]
IniName=optional.ini
InfoName=optional.rtf
Name=Optional
Description=Optional install
");

                File.WriteAllText(Path.Combine(TslPatchDataPath, "changes.ini"), "[Settings]\nLogLevel=3\nWindowCaption=Default Install\n");
                if (includeOptionalIni)
                {
                    File.WriteAllText(Path.Combine(TslPatchDataPath, "optional.ini"), "[Settings]\nLogLevel=4\nWindowCaption=Optional Install\n");
                }
                File.WriteAllText(Path.Combine(TslPatchDataPath, "info.rtf"), "{\\rtf1\\ansi Default}");
                File.WriteAllText(Path.Combine(TslPatchDataPath, "optional.rtf"), "{\\rtf1\\ansi Optional}");
            }

            public void CreateChangesOnlyMod()
            {
                File.WriteAllText(Path.Combine(TslPatchDataPath, "changes.ini"), "[Settings]\nLogLevel=3\nWindowCaption=Changes Only\n");
                File.WriteAllText(Path.Combine(TslPatchDataPath, "info.rtf"), "{\\rtf1\\ansi Default}");
            }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(ModRootPath))
                    {
                        Directory.Delete(ModRootPath, recursive: true);
                    }
                }
                catch
                {
                }
            }
        }

        private sealed class TemporaryCliInstallDirectory : IDisposable
        {
            public string RootPath { get; }
            public string ModRootPath { get; }
            public string TslPatchDataPath { get; }
            public string GameRootPath { get; }

            public TemporaryCliInstallDirectory()
            {
                RootPath = Path.Combine(Path.GetTempPath(), "KPatcher_CliInstall_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                ModRootPath = Path.Combine(RootPath, "mod");
                TslPatchDataPath = Path.Combine(ModRootPath, "tslpatchdata");
                GameRootPath = Path.Combine(RootPath, "game");

                Directory.CreateDirectory(TslPatchDataPath);
                Directory.CreateDirectory(GameRootPath);
                File.WriteAllText(Path.Combine(GameRootPath, "swkotor2.exe"), string.Empty);
            }

            public void CreateInstallableMod()
            {
                File.WriteAllText(Path.Combine(TslPatchDataPath, "changes.ini"), "[Settings]\nLogLevel=3\n");
                File.WriteAllText(Path.Combine(TslPatchDataPath, "info.rtf"), "{\\rtf1\\ansi CLI Test}");
            }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(RootPath))
                    {
                        Directory.Delete(RootPath, true);
                    }
                }
                catch
                {
                }
            }
        }
    }
}
