using System;
using System.Collections.Generic;
using KPatcher.Core.Config;
using KPatcher.Core.Logger;

namespace KEditChanges
{
    /// <summary>
    /// In-memory changes.ini document (ChangeEdit <c>TST_IniFile</c> + <c>PatcherConfig</c> mirror).
    /// </summary>
    public sealed class ChangesIniDocument
    {
        public ChangesIniDocument(
            string sourcePath,
            PatcherConfig config,
            PatchLogger loadLogger)
        {
            SourcePath = sourcePath ?? string.Empty;
            Config = config ?? throw new ArgumentNullException(nameof(config));
            LoadLogger = loadLogger ?? new PatchLogger();
        }

        public string SourcePath { get; }

        public PatcherConfig Config { get; }

        public PatchLogger LoadLogger { get; }

        public bool IsDirty { get; private set; }

        public void MarkClean()
        {
            IsDirty = false;
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }
    }

    /// <summary>
    /// Section counts for tree navigation (ChangeEdit main form).
    /// </summary>
    public sealed class ChangesIniSummary
    {
        public int TlkModifiers { get; set; }
        public int InstallFiles { get; set; }
        public int TwoDaFiles { get; set; }
        public int GffFiles { get; set; }
        public int CompileEntries { get; set; }
        public int HackEntries { get; set; }
        public int SsfFiles { get; set; }
        public int TotalPatches { get; set; }
        public string WindowTitle { get; set; } = string.Empty;
        public int LogLevel { get; set; }
        public bool InstallerMode { get; set; }
    }

    /// <summary>
    /// Tree node kinds matching ChangeEdit section panels.
    /// </summary>
    public enum ChangesIniSectionKind
    {
        Settings,
        TlkList,
        InstallList,
        TwoDaList,
        GffList,
        CompileList,
        HackList,
        SsfList
    }

    public sealed class ChangesIniSectionNode
    {
        public ChangesIniSectionKind Kind { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    public static class ChangesIniSectionCatalog
    {
        public static bool TryParseIniSectionName(string sectionName, out ChangesIniSectionKind kind)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                kind = ChangesIniSectionKind.Settings;
                return false;
            }

            switch (sectionName.Trim().ToLowerInvariant())
            {
                case "settings":
                    kind = ChangesIniSectionKind.Settings;
                    return true;
                case "tlklist":
                    kind = ChangesIniSectionKind.TlkList;
                    return true;
                case "installlist":
                    kind = ChangesIniSectionKind.InstallList;
                    return true;
                case "2dalist":
                    kind = ChangesIniSectionKind.TwoDaList;
                    return true;
                case "gfflist":
                    kind = ChangesIniSectionKind.GffList;
                    return true;
                case "compilelist":
                    kind = ChangesIniSectionKind.CompileList;
                    return true;
                case "hacklist":
                    kind = ChangesIniSectionKind.HackList;
                    return true;
                case "ssflist":
                    kind = ChangesIniSectionKind.SsfList;
                    return true;
                default:
                    kind = ChangesIniSectionKind.Settings;
                    return false;
            }
        }

        public static string ToIniSectionName(ChangesIniSectionKind kind)
        {
            switch (kind)
            {
                case ChangesIniSectionKind.Settings:
                    return "Settings";
                case ChangesIniSectionKind.TlkList:
                    return "TLKList";
                case ChangesIniSectionKind.InstallList:
                    return "InstallList";
                case ChangesIniSectionKind.TwoDaList:
                    return "2DAList";
                case ChangesIniSectionKind.GffList:
                    return "GFFList";
                case ChangesIniSectionKind.CompileList:
                    return "CompileList";
                case ChangesIniSectionKind.HackList:
                    return "HACKList";
                case ChangesIniSectionKind.SsfList:
                    return "SSFList";
                default:
                    return kind.ToString();
            }
        }

        public static List<ChangesIniSectionNode> BuildTree(ChangesIniDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            PatcherConfig config = document.Config;
            return new List<ChangesIniSectionNode>
            {
                new ChangesIniSectionNode
                {
                    Kind = ChangesIniSectionKind.Settings,
                    DisplayName = "Settings",
                    ItemCount = 1
                },
                new ChangesIniSectionNode
                {
                    Kind = ChangesIniSectionKind.TlkList,
                    DisplayName = "TLK List",
                    ItemCount = config.PatchesTLK?.Modifiers.Count ?? 0
                },
                new ChangesIniSectionNode
                {
                    Kind = ChangesIniSectionKind.InstallList,
                    DisplayName = "Install List",
                    ItemCount = config.InstallList?.Count ?? 0
                },
                new ChangesIniSectionNode
                {
                    Kind = ChangesIniSectionKind.TwoDaList,
                    DisplayName = "2DA List",
                    ItemCount = config.Patches2DA?.Count ?? 0
                },
                new ChangesIniSectionNode
                {
                    Kind = ChangesIniSectionKind.GffList,
                    DisplayName = "GFF List",
                    ItemCount = config.PatchesGFF?.Count ?? 0
                },
                new ChangesIniSectionNode
                {
                    Kind = ChangesIniSectionKind.CompileList,
                    DisplayName = "Compile List",
                    ItemCount = config.PatchesNSS?.Count ?? 0
                },
                new ChangesIniSectionNode
                {
                    Kind = ChangesIniSectionKind.HackList,
                    DisplayName = "HACK List",
                    ItemCount = config.PatchesNCS?.Count ?? 0
                },
                new ChangesIniSectionNode
                {
                    Kind = ChangesIniSectionKind.SsfList,
                    DisplayName = "SSF List",
                    ItemCount = config.PatchesSSF?.Count ?? 0
                }
            };
        }
    }
}
