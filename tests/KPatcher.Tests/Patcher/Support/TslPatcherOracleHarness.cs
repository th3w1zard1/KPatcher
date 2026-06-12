using System;
using System.IO;
using KPatcher.Core.Logger;
using KPatcher.Core.Patcher;

namespace KPatcher.Core.Tests.Patcher.Support
{
    /// <summary>
    /// Optional KPatcher vs TSLPatcher.exe oracle helpers. Dual-run diff requires
    /// <c>KPATCHER_TSLPATCHER_EXE</c>; manifest capture supports deterministic KPatcher characterization.
    /// </summary>
    public static class TslPatcherOracleHarness
    {
        public static string ExePath => Environment.GetEnvironmentVariable("KPATCHER_TSLPATCHER_EXE");

        public static bool IsExeConfigured =>
            !string.IsNullOrWhiteSpace(ExePath) && File.Exists(ExePath);

        /// <summary>
        /// When false, optional oracle tests should not run (unset or missing <c>KPATCHER_TSLPATCHER_EXE</c>).
        /// </summary>
        public static bool ShouldRunExeReferenceTier => IsExeConfigured;

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

        public static string CaptureGameFingerprint(string gameRoot)
        {
            return InstallAssertionLadder.ComputeTreeFingerprint(gameRoot);
        }
    }
}
