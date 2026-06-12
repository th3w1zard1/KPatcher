using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KPatcher.Core.Tests.Patcher.Support
{
    /// <summary>
    /// Normalized game-tree manifest: relative path, byte length, SHA-256 per file.
    /// </summary>
    public sealed class InstallManifestSnapshot
    {
        public InstallManifestSnapshot(IReadOnlyList<InstallManifestEntry> entries)
        {
            Entries = entries ?? Array.Empty<InstallManifestEntry>();
        }

        public IReadOnlyList<InstallManifestEntry> Entries { get; }

        public static InstallManifestSnapshot FromFingerprintText(string fingerprintText)
        {
            if (string.IsNullOrWhiteSpace(fingerprintText))
            {
                return new InstallManifestSnapshot(Array.Empty<InstallManifestEntry>());
            }

            var entries = new List<InstallManifestEntry>();
            foreach (string line in fingerprintText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = line.Split('|');
                if (parts.Length != 3)
                {
                    continue;
                }

                entries.Add(new InstallManifestEntry(parts[0], long.Parse(parts[1]), parts[2]));
            }

            return new InstallManifestSnapshot(entries);
        }

        public static InstallManifestSnapshot Capture(string gameRoot)
        {
            return FromFingerprintText(InstallAssertionLadder.ComputeTreeFingerprint(gameRoot));
        }

        public string ToFingerprintText()
        {
            return string.Join(
                "\n",
                Entries.Select(e => e.RelativePath + "|" + e.Length + "|" + e.Sha256Hex));
        }

        public InstallManifestDiff DiffAgainst(InstallManifestSnapshot other)
        {
            var left = Entries.ToDictionary(e => e.RelativePath, StringComparer.OrdinalIgnoreCase);
            var right = other.Entries.ToDictionary(e => e.RelativePath, StringComparer.OrdinalIgnoreCase);

            var onlyLeft = new List<string>();
            var onlyRight = new List<string>();
            var changed = new List<string>();

            foreach (string path in left.Keys.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                if (!right.ContainsKey(path))
                {
                    onlyLeft.Add(path);
                    continue;
                }

                if (!left[path].Equals(right[path]))
                {
                    changed.Add(path);
                }
            }

            foreach (string path in right.Keys.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                if (!left.ContainsKey(path))
                {
                    onlyRight.Add(path);
                }
            }

            return new InstallManifestDiff(onlyLeft, onlyRight, changed);
        }
    }

    public sealed class InstallManifestEntry
    {
        public InstallManifestEntry(string relativePath, long length, string sha256Hex)
        {
            RelativePath = relativePath;
            Length = length;
            Sha256Hex = sha256Hex;
        }

        public string RelativePath { get; }
        public long Length { get; }
        public string Sha256Hex { get; }

        public bool Equals(InstallManifestEntry other)
        {
            if (other == null)
            {
                return false;
            }

            return Length == other.Length
                && string.Equals(Sha256Hex, other.Sha256Hex, StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class InstallManifestDiff
    {
        public InstallManifestDiff(
            IReadOnlyList<string> onlyInLeft,
            IReadOnlyList<string> onlyInRight,
            IReadOnlyList<string> changedPaths)
        {
            OnlyInLeft = onlyInLeft;
            OnlyInRight = onlyInRight;
            ChangedPaths = changedPaths;
        }

        public IReadOnlyList<string> OnlyInLeft { get; }
        public IReadOnlyList<string> OnlyInRight { get; }
        public IReadOnlyList<string> ChangedPaths { get; }

        public bool IsEmpty =>
            OnlyInLeft.Count == 0 && OnlyInRight.Count == 0 && ChangedPaths.Count == 0;

        public string Describe()
        {
            if (IsEmpty)
            {
                return "manifests match";
            }

            var sb = new StringBuilder();
            if (OnlyInLeft.Count > 0)
            {
                sb.AppendLine("only in left:");
                foreach (string path in OnlyInLeft)
                {
                    sb.AppendLine("  " + path);
                }
            }

            if (OnlyInRight.Count > 0)
            {
                sb.AppendLine("only in right:");
                foreach (string path in OnlyInRight)
                {
                    sb.AppendLine("  " + path);
                }
            }

            if (ChangedPaths.Count > 0)
            {
                sb.AppendLine("changed:");
                foreach (string path in ChangedPaths)
                {
                    sb.AppendLine("  " + path);
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
