using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using KPatcher.Core.Tests.Patcher.Support;
using Xunit;
using Xunit.Abstractions;

namespace KPatcher.Core.Tests.Patcher
{
    /// <summary>
    /// Maintainer-only golden capture. Not part of default CI expectations.
    /// </summary>
    public sealed class ScenarioGoldenCaptureTests
    {
        private readonly ITestOutputHelper _output;

        public ScenarioGoldenCaptureTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Capture_AllInlineScenarioFingerprints_WhenEnvSet()
        {
            if (!string.Equals(
                Environment.GetEnvironmentVariable("KP_CAPTURE_SCENARIO_GOLDENS"),
                "1",
                StringComparison.Ordinal))
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Paste into ScenarioGoldenManifests.Fingerprints:");
            foreach (EmbeddedInstallScenario scenario in EmbeddedScenarioDefinitions.RunnableScenarios)
            {
                using (var env = new ModInstallerIntegrationEnvironment("KPatcher_Capture_"))
                {
                    scenario.RunInstall(env);
                    string fingerprint = InstallAssertionLadder.ComputeTreeFingerprint(env.GameRoot);
                    sb.AppendLine("  [\"" + scenario.Id + "\"] = @\"" + fingerprint.Replace("\"", "\"\"") + "\",");
                    _output.WriteLine(scenario.Id);
                    _output.WriteLine(fingerprint);
                }
            }

            string reportPath = Path.Combine(Path.GetTempPath(), "kpatcher-scenario-goldens.txt");
            File.WriteAllText(reportPath, sb.ToString());
            _output.WriteLine("Written: " + reportPath);
        }

        [Fact]
        public void ExportSingleScenarioBaseline_WhenEnvSet()
        {
            if (!string.Equals(
                Environment.GetEnvironmentVariable("KP_EXPORT_ORACLE_BASELINE"),
                "1",
                StringComparison.Ordinal))
            {
                return;
            }

            string scenarioId = Environment.GetEnvironmentVariable("KP_EXPORT_ORACLE_SCENARIO_ID")
                ?? "inline_install_marker";
            string outputPath = Environment.GetEnvironmentVariable("KP_EXPORT_ORACLE_BASELINE_PATH");
            outputPath.Should().NotBeNullOrWhiteSpace();

            EmbeddedInstallScenario scenario = EmbeddedScenarioDefinitions.RunnableScenarios
                .First(s => s.Id == scenarioId);

            using (var env = new ModInstallerIntegrationEnvironment("KPatcher_Export_"))
            {
                InstallManifestSnapshot manifest = TslPatcherOracleHarness.InstallAndCaptureManifest(env, scenario);
                File.WriteAllText(outputPath, manifest.ToFingerprintText());
                _output.WriteLine("Exported: " + outputPath);
            }
        }
    }
}
