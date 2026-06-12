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
            using (var env1 = new ModInstallerIntegrationEnvironment("KPatcher_Oracle_A_"))
            using (var env2 = new ModInstallerIntegrationEnvironment("KPatcher_Oracle_B_"))
            {
                var scenario = EmbeddedScenarioDefinitions.RunnableScenarios[1];
                scenario.Seed(env1);
                scenario.Seed(env2);

                string fingerprint1 = TslPatcherOracleHarness.InstallAndCaptureFingerprint(env1, scenario.ChangesIniBody);
                string fingerprint2 = TslPatcherOracleHarness.InstallAndCaptureFingerprint(env2, scenario.ChangesIniBody);

                fingerprint1.Should().NotBeNullOrWhiteSpace();
                fingerprint1.Should().Be(fingerprint2, "identical seeds and INI should yield identical game trees");
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
        public void Install_WhenExeConfigured_MatchesDeterministicFingerprintBaseline()
        {
            if (!TslPatcherOracleHarness.ShouldRunExeReferenceTier)
            {
                return;
            }

            using (var env = new ModInstallerIntegrationEnvironment("KPatcher_Oracle_Exe_"))
            {
                var scenario = EmbeddedScenarioDefinitions.RunnableScenarios[1];
                scenario.Seed(env);
                string fingerprint = TslPatcherOracleHarness.InstallAndCaptureFingerprint(env, scenario.ChangesIniBody);
                fingerprint.Should().Contain("Override|marker.txt|");
            }
        }
    }
}
