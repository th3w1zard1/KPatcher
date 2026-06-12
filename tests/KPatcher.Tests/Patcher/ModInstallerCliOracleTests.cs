using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using KPatcher;
using KPatcher.Core.Tests.Patcher.Support;
using Xunit;

namespace KPatcher.Core.Tests.Patcher
{
    /// <summary>
    /// Oracle: CLI <c>Program.RunCli --install</c> must produce the same game tree as direct <see cref="ModInstaller.Install"/>.
    /// </summary>
    public sealed class ModInstallerCliOracleTests
    {
        [Theory]
        [MemberData(nameof(OracleScenarioIds))]
        public void Install_CliAndDirectModInstaller_ProduceIdenticalGameManifests(string scenarioId)
        {
            EmbeddedInstallScenario scenario = EmbeddedScenarioDefinitions.RunnableScenarios
                .First(s => s.Id == scenarioId);

            InstallManifestSnapshot direct;
            using (var directEnv = new ModInstallerIntegrationEnvironment("KPatcher_CliOracle_Direct_"))
            {
                direct = TslPatcherOracleHarness.InstallAndCaptureManifest(directEnv, scenario);
            }

            InstallManifestSnapshot cli;
            using (var cliEnv = new ModInstallerIntegrationEnvironment("KPatcher_CliOracle_Cli_"))
            {
                scenario.Seed(cliEnv);
                cliEnv.WriteChangesIni(scenario.ChangesIniBody, scenario.ChangesIniRelative);

                var cliArgs = new List<string>
                {
                    "--install",
                    "--tslpatchdata",
                    cliEnv.ModRoot,
                    "--game-dir",
                    cliEnv.GameRoot
                };
                if (scenario.CliNamespaceOptionIndex.HasValue)
                {
                    cliArgs.Add("--namespace-option-index");
                    cliArgs.Add(scenario.CliNamespaceOptionIndex.Value.ToString());
                }

                var stdout = new StringWriter();
                var stderr = new StringWriter();
                int exitCode = Program.RunCli(
                    KPatcherCLI.ParseArgs(cliArgs.ToArray()),
                    stdout,
                    stderr);

                exitCode.Should().Be(0, stderr.ToString());
                cli = InstallManifestSnapshot.Capture(cliEnv.GameRoot);
            }

            InstallManifestDiff diff = direct.DiffAgainst(cli);
            diff.IsEmpty.Should().BeTrue("CLI vs direct install: " + diff.Describe());
        }

        public static IEnumerable<object[]> OracleScenarioIds()
        {
            foreach (EmbeddedInstallScenario scenario in EmbeddedScenarioDefinitions.RunnableScenarios)
            {
                if (scenario.Id == "inline_settings_only")
                {
                    continue;
                }

                yield return new object[] { scenario.Id };
            }
        }
    }
}
