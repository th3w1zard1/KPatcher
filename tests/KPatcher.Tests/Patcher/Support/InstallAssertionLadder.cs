using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using KPatcher.Core.Formats.NCS;
using KPatcher.Core.Formats.TLK;
using KPatcher.Core.Logger;

namespace KPatcher.Core.Tests.Patcher.Support
{
    /// <summary>
    /// Shared assertion helpers for install-path tests (L0 presence through L3 structure).
    /// </summary>
    public static class InstallAssertionLadder
    {
        public static void AssertFileExistsL0(string path)
        {
            File.Exists(path).Should().BeTrue("expected install output at {0}", path);
        }

        public static void AssertBytesEqualL1(string path, byte[] expected)
        {
            AssertFileExistsL0(path);
            File.ReadAllBytes(path).Should().Equal(expected);
        }

        public static void AssertTextEqualL1(string path, string expected)
        {
            AssertFileExistsL0(path);
            File.ReadAllText(path).Should().Be(expected);
        }

        public static void AssertParsesAsNcsL2(string ncsPath)
        {
            AssertFileExistsL0(ncsPath);
            byte[] bytes = File.ReadAllBytes(ncsPath);
            bytes.Length.Should().BeGreaterThan(8, "NCS file should have header + body");
            NCS ncs = NCSAuto.ReadNcs(bytes);
            ncs.Instructions.Should().NotBeNull();
            ncs.Instructions.Count.Should().BeGreaterThan(0, "compiled NSS should yield instructions");
        }

        public static void AssertTlkStringAtL3(string tlkPath, int stringIndex, string expectedText)
        {
            AssertFileExistsL0(tlkPath);
            TLK tlk = TLK.FromBytes(File.ReadAllBytes(tlkPath));
            tlk.Get(stringIndex).Text.Should().Be(expectedText);
        }

        public static void AssertLoggerContainsFragment(PatchLogger logger, string fragment, bool errors = false, bool warnings = false, bool notes = false)
        {
            fragment.Should().NotBeNullOrWhiteSpace();
            bool found = logger.Errors.Any(e => e.Message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!found && warnings)
            {
                found = logger.Warnings.Any(w => w.Message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (!found && notes)
            {
                found = logger.Notes.Any(n => n.Message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            found.Should().BeTrue("logger should mention '{0}'", fragment);
        }

        public static string ComputeTreeFingerprint(string rootDirectory)
        {
            if (!Directory.Exists(rootDirectory))
            {
                return string.Empty;
            }

            var linesByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string file in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                string relative = file.Substring(rootDirectory.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace('\\', '/');
                byte[] hash;
                using (var sha = SHA256.Create())
                {
                    hash = sha.ComputeHash(File.ReadAllBytes(file));
                }
                string hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                linesByPath[relative] = relative + "|" + new FileInfo(file).Length + "|" + hex;
            }

            return string.Join(
                "\n",
                linesByPath.Keys.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).Select(p => linesByPath[p]));
        }
    }
}
