using System.IO;
using FluentAssertions;
using KPatcher.Core.Tests.Patcher.Support;
using Xunit;

namespace KPatcher.Core.Tests.Patcher
{
    [Trait("Category", "TslPatcherExeReference")]
    public sealed class ModInstallerOracleReferenceTests
    {
        [Fact]
        public void Install_DeterministicInlineScenario_ProducesStableGameFingerprint()
        {
            var scenario = EmbeddedScenarioDefinitions.RunnableScenarios[1];

            using (var env1 = new ModInstallerIntegrationEnvironment("KPatcher_Oracle_A_"))
            using (var env2 = new ModInstallerIntegrationEnvironment("KPatcher_Oracle_B_"))
            {
                InstallManifestSnapshot snap1 = TslPatcherOracleHarness.InstallAndCaptureManifest(env1, scenario);
                InstallManifestSnapshot snap2 = TslPatcherOracleHarness.InstallAndCaptureManifest(env2, scenario);

                snap1.ToFingerprintText().Should().NotBeNullOrWhiteSpace();
                snap1.DiffAgainst(snap2).IsEmpty.Should().BeTrue();
            }
        }

        [Fact]
        public void ReferenceExe_WhenConfigured_IsAvailableForGoldenComparisons()
        {
            if (!TslPatcherOracleHarness.ShouldRunExeReferenceTier)
            {
                return;
            }

            TslPatcherOracleHarness.ExePath.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void Install_WhenExeConfigured_PackageLayoutBesideExeIsValid()
        {
            if (!TslPatcherOracleHarness.ShouldRunExeReferenceTier)
            {
                return;
            }

            using (var env = new ModInstallerIntegrationEnvironment("KPatcher_Oracle_Layout_"))
            {
                var scenario = EmbeddedScenarioDefinitions.RunnableScenarios[1];
                scenario.Seed(env);
                env.WriteChangesIni(scenario.ChangesIniBody, scenario.ChangesIniRelative);

                string layoutDir = TslPatcherOracleHarness.PrepareExeModLayout(env, env.TempRoot);
                File.Exists(Path.Combine(layoutDir, Path.GetFileName(TslPatcherOracleHarness.ExePath))).Should().BeTrue();
                File.Exists(Path.Combine(layoutDir, "tslpatchdata", "changes.ini")).Should().BeTrue();
                File.Exists(Path.Combine(layoutDir, "tslpatchdata", "info.rtf")).Should().BeTrue();
            }
        }

        [Fact]
        public void Install_WhenManifestBaselineConfigured_MatchesBaselineFile()
        {
            if (!TslPatcherOracleHarness.ShouldRunManifestBaselineTier)
            {
                return;
            }

            var scenario = EmbeddedScenarioDefinitions.RunnableScenarios[1];
            using (var env = new ModInstallerIntegrationEnvironment("KPatcher_Oracle_Baseline_"))
            {
                InstallManifestSnapshot actual = TslPatcherOracleHarness.InstallAndCaptureManifest(env, scenario);
                InstallManifestDiff diff = TslPatcherOracleHarness.CompareToBaselineFile(
                    actual,
                    TslPatcherOracleHarness.ManifestBaselinePath);
                diff.IsEmpty.Should().BeTrue(diff.Describe());
            }
        }
    }
}
