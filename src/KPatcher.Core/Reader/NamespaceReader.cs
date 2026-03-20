using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IniParser;
using IniParser.Model;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Namespaces;

namespace KPatcher.Core.Reader
{

    /// <summary>
    /// Responsible for reading and loading namespaces from the namespaces.ini file.
    /// </summary>
    public class NamespaceReader
    {
        private readonly IniData _ini;

        public List<PatcherNamespace> Namespaces { get; } = new List<PatcherNamespace>();

        public NamespaceReader(IniData ini)
        {
            _ini = ini ?? throw new ArgumentNullException(nameof(ini));
        }

        /// <summary>
        /// Loads namespaces from a config file at the specified path (.ini or .yaml/.yml).
        /// YAML uses the same section/key structure as INI (see ConfigReaderYaml).
        /// </summary>
        public static List<PatcherNamespace> FromFilePath(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                throw new FileNotFoundException("Namespaces config file not found.", path);
            }

            string ext = System.IO.Path.GetExtension(path);
            bool isYaml = ext.Equals(".yaml", StringComparison.OrdinalIgnoreCase) || ext.Equals(".yml", StringComparison.OrdinalIgnoreCase);

            IniData ini = isYaml
                ? ConfigReaderYaml.LoadAndParseYaml(path)
                : ConfigReader.LoadAndParseIni(path, caseInsensitive: true);

            return new NamespaceReader(ini).Load();
        }

        /// <summary>
        /// Load and parse all namespaces from the INI data.
        /// </summary>
        public List<PatcherNamespace> Load()
        {
            // Find the [Namespaces] section (case-insensitive)
            SectionData namespacesSection = _ini.Sections.FirstOrDefault(s =>
                s.SectionName.Equals("Namespaces", StringComparison.OrdinalIgnoreCase));

            if (namespacesSection is null)
            {
                throw new KeyNotFoundException("The [Namespaces] section was not found in the ini");
            }

            var namespaces = new List<PatcherNamespace>();

            foreach (KeyData keyData in namespacesSection.Keys)
            {
                string namespaceId = keyData.Value;

                // Find the section for this namespace (case-insensitive)
                SectionData namespaceSection = _ini.Sections.FirstOrDefault(s =>
                    s.SectionName.Equals(namespaceId, StringComparison.OrdinalIgnoreCase)) ?? throw new KeyNotFoundException(
                        $"The '[{namespaceId}]' section was not found in the 'namespaces.ini' file, " +
                        $"referenced by '{keyData.KeyName}={namespaceId}' in [{namespacesSection.SectionName}].");

                // Required fields

                string iniFilename = GetValue(namespaceSection, "IniName")
                    ?? throw new KeyNotFoundException($"IniName not found in [{namespaceSection.SectionName}]");
                string infoFilename = GetValue(namespaceSection, "InfoName")
                    ?? throw new KeyNotFoundException($"InfoName not found in [{namespaceSection.SectionName}]");

                var ns = new PatcherNamespace(iniFilename, infoFilename)
                {
                    // Optional fields
                    DataFolderPath = GetValue(namespaceSection, "DataPath") ?? string.Empty,
                    Name = GetValue(namespaceSection, "Name")?.Trim() ?? string.Empty,
                    Description = GetValue(namespaceSection, "Description") ?? string.Empty,
                    NamespaceId = namespaceSection.SectionName
                };

                namespaces.Add(ns);
            }

            return namespaces;
        }

        [CanBeNull]
        private static string GetValue(SectionData section, string key)
        {
            return section.Keys.FirstOrDefault(k =>
                k.KeyName.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;
        }
    }
}

