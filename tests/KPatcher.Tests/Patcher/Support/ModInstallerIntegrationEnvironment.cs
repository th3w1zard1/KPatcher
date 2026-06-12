using System;
using System.IO;
using KPatcher.Core.Logger;
using KPatcher.Core.Patcher;

namespace KPatcher.Core.Tests.Patcher.Support
{
    /// <summary>
    /// Ephemeral mod + game tree for install-path integration tests (no committed fixtures).
    /// </summary>
    public sealed class ModInstallerIntegrationEnvironment : IDisposable
    {
        public string TempRoot { get; }
        public string ModRoot { get; }
        public string GameRoot { get; }
        public string TslPatchDataPath { get; }
        public string OverridePath { get; }
        public string ModulesPath { get; }

        public ModInstallerIntegrationEnvironment(string namePrefix = "KPatcher_Int_")
        {
            TempRoot = Path.Combine(Path.GetTempPath(), namePrefix + Guid.NewGuid().ToString("N"));
            ModRoot = Path.Combine(TempRoot, "mod");
            GameRoot = Path.Combine(TempRoot, "game");
            TslPatchDataPath = Path.Combine(ModRoot, "tslpatchdata");
            OverridePath = Path.Combine(GameRoot, "Override");
            ModulesPath = Path.Combine(GameRoot, "Modules");
            Directory.CreateDirectory(TslPatchDataPath);
            Directory.CreateDirectory(OverridePath);
            Directory.CreateDirectory(ModulesPath);
            File.WriteAllText(Path.Combine(GameRoot, "swkotor2.exe"), string.Empty);
        }

        public ModInstaller CreateInstaller(PatchLogger logger = null, string changesIniRelativeToTslPatchData = "changes.ini")
        {
            string iniPath = Path.Combine(TslPatchDataPath, changesIniRelativeToTslPatchData.Replace('/', Path.DirectorySeparatorChar));
            return new ModInstaller(ModRoot, GameRoot, iniPath, logger ?? new PatchLogger());
        }

        public void WriteChangesIni(string body, string relativePath = "changes.ini")
        {
            string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            string fullPath = Path.Combine(TslPatchDataPath, normalized);
            string parent = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.WriteAllText(fullPath, body);
        }

        public void Dispose()
        {
            if (Directory.Exists(TempRoot))
            {
                try
                {
                    Directory.Delete(TempRoot, true);
                }
                catch
                {
                }
            }
        }
    }
}
