using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using KPatcher.Core.Config;
using KPatcher.Core.Mods;
using KEditChanges;

namespace KEditChanges.Ini
{
    /// <summary>
    /// Writes a TSLPatcher-compatible changes.ini from <see cref="PatcherConfig"/>.
    /// Settings use ChangeEdit TST escapes; patch lists delegate to <see cref="KPatcherINISerializer"/>.
    /// </summary>
    public sealed class ChangeEditIniWriter
    {
        private readonly KPatcherINISerializer _patchSerializer = new KPatcherINISerializer();

        public string Serialize(PatcherConfig config, bool includeHeader = false, bool verbose = true)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var lines = new List<string>();
            if (includeHeader)
            {
                lines.AddRange(GenerateHeaderComment());
            }

            lines.AddRange(WriteSettingsSection(config));
            lines.Add("");

            string patchBody = _patchSerializer.Serialize(
                PatcherConfigMapper.ToModificationsByType(config),
                includeHeader: false,
                includeSettings: false,
                verbose: verbose);

            if (!string.IsNullOrEmpty(patchBody))
            {
                lines.Add(patchBody.TrimEnd());
            }

            return string.Join("\n", lines).TrimEnd() + "\n";
        }

        public void WriteToFile(PatcherConfig config, string filePath, bool includeHeader = false, bool verbose = true)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("filePath is required", nameof(filePath));
            }

            string text = Serialize(config, includeHeader, verbose);
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, text, Encoding.UTF8);
        }

        private static List<string> GenerateHeaderComment()
        {
            string today = DateTime.UtcNow.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            return new List<string>
            {
                "; ============================================================================",
                ";  changes.ini — edited with KEditChanges (" + today + ")",
                ";  Port of OpenKotOR/ChangeEdit — patch engine: KPatcher.Core",
                "; ============================================================================",
                ""
            };
        }

        private static List<string> WriteSettingsSection(PatcherConfig config)
        {
            var lines = new List<string> { "[Settings]" };

            if (!string.IsNullOrEmpty(config.WindowTitle))
            {
                lines.Add("WindowCaption=" + TstIniEscapes.Encode(config.WindowTitle));
            }

            if (!string.IsNullOrEmpty(config.ConfirmMessage))
            {
                lines.Add("ConfirmMessage=" + TstIniEscapes.Encode(config.ConfirmMessage));
            }

            for (int i = 0; i < config.RequiredFiles.Count; i++)
            {
                string key = i == 0 ? "Required" : "Required" + i.ToString(CultureInfo.InvariantCulture);
                string[] files = config.RequiredFiles[i];
                if (files != null && files.Length > 0)
                {
                    lines.Add(key + "=" + string.Join(",", files));
                }
            }

            for (int i = 0; i < config.RequiredMessages.Count; i++)
            {
                string key = i == 0 ? "RequiredMsg" : "RequiredMsg" + i.ToString(CultureInfo.InvariantCulture);
                string message = config.RequiredMessages[i] ?? string.Empty;
                lines.Add(key + "=" + TstIniEscapes.Encode(message));
            }

            lines.Add("SaveProcessedScripts=" + config.SaveProcessedScripts.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(config.ScriptCompilerFlags))
            {
                lines.Add("ScriptCompilerFlags=" + config.ScriptCompilerFlags);
            }

            lines.Add("LogLevel=" + ((int)config.LogLevel).ToString(CultureInfo.InvariantCulture));

            if (config.InstallerMode)
            {
                lines.Add("InstallerMode=1");
            }

            if (!config.BackupFiles)
            {
                lines.Add("BackupFiles=0");
            }

            if (config.PlaintextLog)
            {
                lines.Add("PlaintextLog=1");
            }

            if (config.IgnoreFileExtensions)
            {
                lines.Add("IgnoreExtensions=1");
            }

            if (config.GameNumber.HasValue)
            {
                lines.Add("LookupGameNumber=" + config.GameNumber.Value.ToString(CultureInfo.InvariantCulture));
            }

            return lines;
        }
    }
}
