using System;
using System.Collections.Generic;
using System.Globalization;
using IniParser.Model;
using System.IO;
using System.Linq;
using KPatcher.Core.Config;
using KPatcher.Core.Logger;
using KPatcher.Core.Reader;
using KEditChanges.Ini;

namespace KEditChanges
{
  /// <summary>
  /// Load, validate, summarize, and save changes.ini (ChangeEdit core workflow).
  /// </summary>
    public sealed class ChangesIniService
    {
        private readonly ChangeEditIniWriter _writer = new ChangeEditIniWriter();

        public ChangesIniDocument Load(string changesIniPath, string tslPatchDataPath = null)
        {
            if (string.IsNullOrWhiteSpace(changesIniPath))
            {
                throw new ArgumentException("changesIniPath is required", nameof(changesIniPath));
            }

            if (!File.Exists(changesIniPath))
            {
                throw new FileNotFoundException("changes.ini not found: " + changesIniPath, changesIniPath);
            }

            var log = new PatchLogger();
            ConfigReader reader = ConfigReader.FromFilePath(changesIniPath, log, tslPatchDataPath);
            reader.Load(reader.Config);
            return new ChangesIniDocument(Path.GetFullPath(changesIniPath), reader.Config, log);
        }

        public ChangesIniDocument LoadFromIniText(string iniText, string modDirectory = null, string tslPatchDataPath = null)
        {
            if (string.IsNullOrEmpty(iniText))
            {
                throw new ArgumentException("iniText is required", nameof(iniText));
            }

            string modPath = modDirectory ?? Directory.GetCurrentDirectory();
            var log = new PatchLogger();
            IniData ini = ConfigReader.ParseIniText(iniText, caseInsensitive: false, sourcePath: "inline");
            var reader = new ConfigReader(ini, modPath, log, tslPatchDataPath);
            var config = new PatcherConfig();
            reader.Load(config);
            return new ChangesIniDocument(string.Empty, config, log);
        }

        public ChangesIniSummary BuildSummary(ChangesIniDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            PatcherConfig config = document.Config;
            return new ChangesIniSummary
            {
                WindowTitle = config.WindowTitle ?? string.Empty,
                LogLevel = (int)config.LogLevel,
                InstallerMode = config.InstallerMode,
                TlkModifiers = config.PatchesTLK?.Modifiers.Count ?? 0,
                InstallFiles = config.InstallList?.Count ?? 0,
                TwoDaFiles = config.Patches2DA?.Count ?? 0,
                GffFiles = config.PatchesGFF?.Count ?? 0,
                CompileEntries = config.PatchesNSS?.Count ?? 0,
                HackEntries = config.PatchesNCS?.Count ?? 0,
                SsfFiles = config.PatchesSSF?.Count ?? 0,
                TotalPatches = config.PatchCount()
            };
        }

        public ValidationResult Validate(ChangesIniDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var result = new ValidationResult { IsValid = true };
            foreach (PatchLog error in document.LoadLogger.Errors)
            {
                result.IsValid = false;
                result.Messages.Add("ERROR: " + error.Message);
            }

            foreach (PatchLog warning in document.LoadLogger.Warnings)
            {
                result.Messages.Add("WARNING: " + warning.Message);
            }

            if (document.Config.RequiredFiles.Count != document.Config.RequiredMessages.Count)
            {
                result.Messages.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "WARNING: Required count ({0}) != RequiredMsg count ({1})",
                    document.Config.RequiredFiles.Count,
                    document.Config.RequiredMessages.Count));
            }

            return result;
        }

        public string Serialize(ChangesIniDocument document, bool includeHeader = false, bool verbose = true)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return _writer.Serialize(document.Config, includeHeader, verbose);
        }

        public void Save(ChangesIniDocument document, string outputPath, bool includeHeader = false, bool verbose = true)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            _writer.WriteToFile(document.Config, outputPath, includeHeader, verbose);
            document.MarkClean();
        }

        public ValidationResult ValidateFile(string changesIniPath, string tslPatchDataPath = null)
        {
            ChangesIniDocument document = Load(changesIniPath, tslPatchDataPath);
            return Validate(document);
        }
    }

    public sealed class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Messages { get; } = new List<string>();
    }
}
