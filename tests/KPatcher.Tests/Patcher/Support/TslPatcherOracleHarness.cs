using System;
using System.IO;
using KPatcher.Core.Logger;
using KPatcher.Core.Patcher;

namespace KPatcher.Core.Tests.Patcher.Support
{
    /// <summary>
    /// Oracle helpers: deterministic KPatcher fingerprints, optional manifest baseline diff, and
    /// optional <c>KPATCHER_TSLPATCHER_EXE</c> tier (exe is GUI-only — baseline file comparison is the supported path).
    /// </summary>
    public static class TslPatcherOracleHarness
    {
        public static string ExePath => Environment.GetEnvironmentVariable("KPATCHER_TSLPATCHER_EXE");

        public static string ManifestBaselinePath => Environment.GetEnvironmentVariable("KPATCHER_ORACLE_MANIFEST_BASELINE");

        public static bool IsExeConfigured =>
            !string.IsNullOrWhiteSpace(ExePath) && File.Exists(ExePath);

        public static bool HasManifestBaseline =>
            !string.IsNullOrWhiteSpace(ManifestBaselinePath) && File.Exists(ManifestBaselinePath);

        public static bool ShouldRunExeReferenceTier => IsExeConfigured;

        public static bool ShouldRunManifestBaselineTier => HasManifestBaseline;

        public static string InstallAndCaptureFingerprint(
            ModInstallerIntegrationEnvironment env,
            string changesIniBody,
            string changesIniRelative = "changes.ini")
        {
            env.WriteChangesIni(changesIniBody, changesIniRelative);
            var installer = env.CreateInstaller(new PatchLogger(), changesIniRelative);
            installer.Install();
            return InstallAssertionLadder.ComputeTreeFingerprint(env.GameRoot);
        }

        public static InstallManifestSnapshot InstallAndCaptureManifest(
            ModInstallerIntegrationEnvironment env,
            EmbeddedInstallScenario scenario)
        {
            scenario.RunInstall(env);
            return InstallManifestSnapshot.Capture(env.GameRoot);
        }

        public static InstallManifestSnapshot InstallAndCaptureManifest(
            ModInstallerIntegrationEnvironment env,
            string changesIniBody,
            string changesIniRelative = "changes.ini")
        {
            InstallAndCaptureFingerprint(env, changesIniBody, changesIniRelative);
            return InstallManifestSnapshot.Capture(env.GameRoot);
        }

        public static string CaptureGameFingerprint(string gameRoot)
        {
            return InstallAssertionLadder.ComputeTreeFingerprint(gameRoot);
        }

        public static InstallManifestDiff CompareToBaselineFile(InstallManifestSnapshot actual, string baselineFilePath)
        {
            string baselineText = File.ReadAllText(baselineFilePath);
            InstallManifestSnapshot expected = InstallManifestSnapshot.FromFingerprintText(baselineText);
            return actual.DiffAgainst(expected);
        }

        /// <summary>
        /// Copies mod layout beside TSLPatcher.exe (tslpatchdata next to exe). Does not run the GUI installer.
        /// </summary>
        public static string PrepareExeModLayout(ModInstallerIntegrationEnvironment env, string workingRoot)
        {
            string exeDir = Path.Combine(workingRoot, "tslpatcher_exe_layout");
            Directory.CreateDirectory(exeDir);

            string exeName = Path.GetFileName(ExePath);
            string exeDest = Path.Combine(exeDir, exeName);
            File.Copy(ExePath, exeDest, overwrite: true);

            string tslDest = Path.Combine(exeDir, "tslpatchdata");
            CopyDirectory(env.TslPatchDataPath, tslDest);
            File.WriteAllText(Path.Combine(tslDest, "info.rtf"), "{\\rtf1\\ansi Inline oracle scenario}");

            return exeDir;
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string target = Path.Combine(destination, relative);
                string parent = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                File.Copy(file, target, overwrite: true);
            }
        }
    }
}
