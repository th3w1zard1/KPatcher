using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.NCS;
using KPatcher.Core.Formats.NCS.Decompiler;
using Xunit;

namespace KPatcher.Core.Tests.Formats
{
    /// <summary>
    /// Managed-only vanilla coverage (/<c>lfg</c>): NSS -> <see cref="NCSAuto.CompileNss"/> -> full
    /// <see cref="NCSManagedDecompiler.DecompileToNss"/> -> recompile -> <see cref="NcsRoundTripAssertHelpers.AssertNcsStructurallyEqual"/>.
    /// Does not use <c>nwnnsscomp.exe</c>. Requires <c>vendor/Vanilla_KOTOR_Script_Source</c> submodule; when absent, tests return immediately.
    /// </summary>
    [Trait("Category", "Vendor")]
    [Trait("Category", "ManagedNcs")]
    public sealed class VanillaNssManagedDecompileRoundTripTests
    {
        private const int MaxFilesK1 = 60;
        private const int MaxFilesTsl = 200;
        private const int MinStructuralSuccessesK1 = 2;
        private const int MinStructuralSuccessesTsl = 1;

        private static IEnumerable<string> EnumerateNss(string gameDir, int max)
        {
            string root = VanillaNSSCompileTests.VanillaScriptSourceRoot;
            if (string.IsNullOrEmpty(root))
            {
                yield break;
            }

            string dir = Path.Combine(root, gameDir);
            if (!Directory.Exists(dir))
            {
                yield break;
            }

            int n = 0;
            foreach (string path in Directory.EnumerateFiles(dir, "*.nss", SearchOption.AllDirectories))
            {
                if (n >= max)
                {
                    yield break;
                }

                yield return path;
                n++;
            }
        }

        [Fact]
        public void Vanilla_K1_ManagedDecompile_And_StructuralRecompile_WhenSubmodulePresent()
        {
            if (!VanillaNSSCompileTests.VanillaSubmodulePresent)
            {
                return;
            }

            var paths = EnumerateNss("K1", MaxFilesK1).ToList();
            paths.Should().NotBeEmpty("K1 .nss files expected when vanilla submodule is present");

            int ok = 0;
            var failures = new List<string>();

            foreach (string path in paths)
            {
                try
                {
                    string nss = File.ReadAllText(path);
                    if (string.IsNullOrWhiteSpace(nss))
                    {
                        continue;
                    }

                    NCS ncs1 = NCSAuto.CompileNss(nss, Game.K1, null, null, null);
                    string dec = NCSManagedDecompiler.DecompileToNss(ncs1, tsl: false);
                    dec.Should().NotBeNullOrWhiteSpace($"decompiled NSS empty: {path}");

                    NCS ncs2 = NCSAuto.CompileNss(dec, Game.K1, null, null, null);
                    NcsRoundTripAssertHelpers.AssertNcsStructurallyEqual(ncs1, ncs2, path);
                    ok++;
                }
                catch (Exception ex)
                {
                    failures.Add($"{Path.GetFileName(path)}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            ok.Should().BeGreaterOrEqualTo(
                MinStructuralSuccessesK1,
                $"need at least {MinStructuralSuccessesK1} K1 scripts that compile, decompile, and structurally match after recompile. " +
                $"Sample failures: {string.Join(" | ", failures.Take(8))}");
        }

        [Fact]
        public void Vanilla_TSL_ManagedDecompile_And_StructuralRecompile_WhenSubmodulePresent()
        {
            if (!VanillaNSSCompileTests.VanillaSubmodulePresent)
            {
                return;
            }

            var paths = EnumerateNss("TSL", MaxFilesTsl).ToList();
            paths.Should().NotBeEmpty("TSL .nss files expected when vanilla submodule is present");

            int ok = 0;
            var failures = new List<string>();

            foreach (string path in paths)
            {
                try
                {
                    string nss = File.ReadAllText(path);
                    if (string.IsNullOrWhiteSpace(nss))
                    {
                        continue;
                    }

                    NCS ncs1 = NCSAuto.CompileNss(nss, Game.TSL, null, null, null);
                    string dec = NCSManagedDecompiler.DecompileToNss(ncs1, tsl: true);
                    dec.Should().NotBeNullOrWhiteSpace($"decompiled NSS empty: {path}");

                    NCS ncs2 = NCSAuto.CompileNss(dec, Game.TSL, null, null, null);
                    NcsRoundTripAssertHelpers.AssertNcsStructurallyEqual(ncs1, ncs2, path);
                    ok++;
                }
                catch (Exception ex)
                {
                    failures.Add($"{Path.GetFileName(path)}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            ok.Should().BeGreaterOrEqualTo(
                MinStructuralSuccessesTsl,
                $"need at least {MinStructuralSuccessesTsl} TSL script(s) that compile, decompile, and structurally match after recompile " +
                $"(scanned up to {MaxFilesTsl} files). Sample failures: {string.Join(" | ", failures.Take(8))}");
        }
    }
}
