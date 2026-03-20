using System;
using System.Collections.Generic;
using System.IO;
using IniParser.Model;
using YamlDotNet.Serialization;

namespace KPatcher.Core.Reader
{
    /// <summary>
    /// YAML equivalent of INI for namespaces.ini-style config.
    /// Converts between IniData (ini-parser) and YAML for NamespaceReader.
    /// Used to generate .yaml when loading .ini and to load .yaml as config.
    /// </summary>
    public static class NamespaceReaderYaml
    {
        private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();

        private static readonly ISerializer YamlSerializer = new SerializerBuilder()
            .Build();

        /// <summary>
        /// Converts IniData to a YAML-serializable structure (section name -> list of Key/Value).
        /// </summary>
        public static Dictionary<string, List<IniKeyValue>> IniDataToYamlStructure(IniData ini)
        {
            if (ini == null)
            {
                throw new ArgumentNullException(nameof(ini));
            }

            var structure = new Dictionary<string, List<IniKeyValue>>(StringComparer.Ordinal);
            foreach (SectionData section in ini.Sections)
            {
                var list = new List<IniKeyValue>();
                foreach (KeyData keyData in section.Keys)
                {
                    list.Add(new IniKeyValue
                    {
                        Key = keyData.KeyName,
                        Value = keyData.Value ?? string.Empty
                    });
                }
                structure[section.SectionName] = list;
            }
            return structure;
        }

        /// <summary>
        /// Builds IniData from a YAML structure (section name -> list of Key/Value).
        /// </summary>
        public static IniData YamlStructureToIniData(Dictionary<string, List<IniKeyValue>> structure)
        {
            if (structure == null)
            {
                throw new ArgumentNullException(nameof(structure));
            }

            var ini = new IniData();
            foreach (KeyValuePair<string, List<IniKeyValue>> kv in structure)
            {
                string sectionName = kv.Key;
                var sectionData = new SectionData(sectionName);
                foreach (IniKeyValue entry in kv.Value ?? new List<IniKeyValue>())
                {
                    if (entry?.Key == null)
                    {
                        continue;
                    }
                    var keyData = new KeyData(entry.Key) { Value = entry?.Value ?? string.Empty };
                    sectionData.Keys.AddKey(keyData);
                }
                ini.Sections.Add(sectionData);
            }
            return ini;
        }

        /// <summary>
        /// Loads and parses a YAML file into IniData (same structure as INI for NamespaceReader).
        /// </summary>
        public static IniData LoadAndParseYaml(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"YAML file not found: {filePath}", filePath);
            }

            string yamlText = File.ReadAllText(filePath);
            return ParseYamlText(yamlText);
        }

        /// <summary>
        /// Parses YAML text into IniData.
        /// </summary>
        public static IniData ParseYamlText(string yamlText)
        {
            if (string.IsNullOrEmpty(yamlText))
            {
                throw new ArgumentException("YAML text cannot be null or empty", nameof(yamlText));
            }

            // Deserialize to Dictionary<string, List<IniKeyValue>>
            // YamlDotNet with UnderscoredNamingConvention will map "Key"/"Value" to "key"/"value"
            var structure = YamlDeserializer.Deserialize<Dictionary<string, List<IniKeyValue>>>(yamlText);
            if (structure == null)
            {
                throw new InvalidOperationException("YAML deserialized to null.");
            }
            return YamlStructureToIniData(structure);
        }

        /// <summary>
        /// Serializes IniData to YAML and writes to the given path.
        /// </summary>
        public static void WriteIniDataToYaml(IniData ini, string yamlFilePath)
        {
            if (ini == null)
            {
                throw new ArgumentNullException(nameof(ini));
            }
            if (string.IsNullOrEmpty(yamlFilePath))
            {
                throw new ArgumentException("YAML file path cannot be null or empty", nameof(yamlFilePath));
            }

            Dictionary<string, List<IniKeyValue>> structure = IniDataToYamlStructure(ini);
            string yaml = YamlSerializer.Serialize(structure);
            string directory = Path.GetDirectoryName(yamlFilePath) ?? ".";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(yamlFilePath, yaml);
        }
    }
}
