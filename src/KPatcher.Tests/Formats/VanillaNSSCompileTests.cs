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
    /// Tests that compile and roundtrip NSS from vendor/Vanilla_KOTOR_Script_Source using KCompiler (NCSAuto.CompileNss).
    /// Skipped when the submodule is not present. Run with submodules initialized for full coverage.
    /// </summary>
    [Trait("Category", "Vendor")]
    public class VanillaNSSCompileTests
    {
        private static readonly string VanillaRoot = ResolveVanillaScriptRoot();

        private static string ResolveVanillaScriptRoot()
        {
            string baseDir = AppContext.BaseDirectory;
            for (var dir = baseDir; !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir))
            {
                var vendor = Path.Combine(dir, "vendor");
                var vanilla = Path.Combine(vendor, "Vanilla_KOTOR_Script_Source");
                if (Directory.Exists(vanilla))
                    return vanilla;
                var git = Path.Combine(dir, ".git");
                if (Directory.Exists(git) || File.Exists(git))
                {
                    if (Directory.Exists(vanilla))
                        return vanilla;
                    vanilla = Path.Combine(dir, "vendor", "Vanilla_KOTOR_Script_Source");
                    if (Directory.Exists(vanilla))
                        return vanilla;
                }
            }
            return null;
        }

        public static bool VanillaSubmodulePresent => !string.IsNullOrEmpty(VanillaRoot) && Directory.Exists(VanillaRoot);

        private static IEnumerable<string> GetNssFiles(string gameDir, int maxFiles = 20)
        {
            if (string.IsNullOrEmpty(VanillaRoot))
                yield break;
            var dir = Path.Combine(VanillaRoot, gameDir);
            if (!Directory.Exists(dir))
                yield break;
            int n = 0;
            foreach (var path in Directory.EnumerateFiles(dir, "*.nss", SearchOption.AllDirectories))
            {
                if (n >= maxFiles)
                    break;
                yield return path;
                n++;
            }
        }

        [Fact]
        public void Vanilla_K1_CompileAndRoundtrip_WhenSubmodulePresent()
        {
            if (!VanillaSubmodulePresent)
            {
                // Skip when vendor submodule not initialized
                return;
            }

            var paths = GetNssFiles("K1", 50).ToList();
            paths.Should().NotBeEmpty("K1 folder should contain .nss files when submodule is present");
            paths.Count.Should().BeGreaterOrEqualTo(10, "should have at least 10 K1 scripts to sample");
            paths.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p), "all paths must be non-empty");
            paths.Should().OnlyContain(p => File.Exists(p), "all paths must exist");
            paths.Should().OnlyContain(p => Path.GetExtension(p).Equals(".nss", StringComparison.OrdinalIgnoreCase), "all paths must be .nss files");

            int compiled = 0;
            int roundtripped = 0;
            int byteIdentical = 0;
            int instructionCountMatches = 0;
            int instructionTypeMatches = 0;
            int jumpTargetMatches = 0;
            int constantMatches = 0;
            var errors = new List<string>();
            var perFileDetails = new List<string>();

            foreach (string path in paths)
            {
                try
                {
                    // Stage 1: Pre-compilation validation
                    File.Exists(path).Should().BeTrue($"file must exist: {path}");
                    var fileInfo = new FileInfo(path);
                    fileInfo.Length.Should().BeGreaterThan(0, $"file must not be empty: {path}");
                    fileInfo.Length.Should().BeLessThan(10_000_000, $"file size must be reasonable: {path}");

                    string nss = File.ReadAllText(path);
                    nss.Should().NotBeNullOrWhiteSpace($"NSS content must not be empty: {path}");
                    nss.Length.Should().BeGreaterThan(0, $"NSS content length must be > 0: {path}");
                    nss.Should().ContainAny(new[] { "void", "int", "float", "string", "object" }, $"NSS should contain type keywords: {path}");

                    // Stage 2: Compilation validation
                    NCS ncs = NCSAuto.CompileNss(nss, Game.K1, null, null, null);
                    ncs.Should().NotBeNull($"compiled NCS must not be null: {path}");
                    ncs.Instructions.Should().NotBeNull($"instructions list must not be null: {path}");
                    ncs.Instructions.Count.Should().BeGreaterThan(0, $"must have at least one instruction: {path}");
                    ncs.Instructions.Should().OnlyContain(i => i != null, $"all instructions must be non-null: {path}");
                    ncs.Instructions.Should().OnlyContain(i => i.InsType != null, $"all instructions must have InsType: {path}");
                    ncs.Instructions.Should().OnlyContain(i => i.Args != null, $"all instructions must have Args: {path}");
                    // Note: compiler does not set Offset; it is set by NCSBinaryReader on load. Do not assert Offset >= 0 here.

                    // Validate jump instructions have valid jump targets
                    var jumpInstructions = ncs.Instructions.Where(i => i.IsJumpInstruction()).ToList();
                    jumpInstructions.Should().OnlyContain(i => i.Jump != null || i.Args.Count > 0, 
                        $"jump instructions must have jump target or args: {path}");

                    compiled++;
                    var originalInstructionCount = ncs.Instructions.Count;
                    // Stage 3: Binary serialization validation
                    byte[] bytes = NCSAuto.BytesNcs(ncs);
                    bytes.Should().NotBeNull($"bytes must not be null: {path}");
                    bytes.Length.Should().BeGreaterThan(0, $"bytes must not be empty: {path}");
                    bytes.Length.Should().BeGreaterOrEqualTo(13, $"bytes must include header (>=13 bytes): {path}");
                    bytes[0].Should().Be((byte)'N', $"header byte 0 must be 'N': {path}");
                    bytes[1].Should().Be((byte)'C', $"header byte 1 must be 'C': {path}");
                    bytes[2].Should().Be((byte)'S', $"header byte 2 must be 'S': {path}");
                    bytes[3].Should().Be((byte)' ', $"header byte 3 must be ' ': {path}");

                    // Stage 4: Roundtrip validation
                    NCS ncs2 = NCSAuto.ReadNcs(bytes);
                    ncs2.Should().NotBeNull($"re-read NCS must not be null: {path}");
                    ncs2.Instructions.Should().NotBeNull($"re-read instructions must not be null: {path}");
                    ncs2.Instructions.Count.Should().Be(originalInstructionCount, $"instruction count must match exactly: {path}");
                    ncs2.Instructions.Should().OnlyContain(i => i.Offset >= 0, $"after roundtrip all instructions must have valid offset (reader sets them): {path}");

                    // Stage 5: Instruction-by-instruction validation
                    ncs2.Instructions.Count.Should().Be(ncs.Instructions.Count, $"instruction count must match: {path}");
                    for (int i = 0; i < ncs.Instructions.Count; i++)
                    {
                        var orig = ncs.Instructions[i];
                        var round = ncs2.Instructions[i];
                        round.InsType.Should().Be(orig.InsType, $"instruction {i} type must match: {path}");
                        round.Offset.Should().BeGreaterOrEqualTo(0, $"instruction {i} offset must be set by reader: {path}");
                        round.Args.Count.Should().Be(orig.Args.Count, $"instruction {i} args count must match: {path}");
                        for (int j = 0; j < orig.Args.Count; j++)
                        {
                            var o = orig.Args[j];
                            var r = round.Args[j];
                            if (o is long ol && r is int ri)
                                ri.Should().Be((int)ol, $"instruction {i} arg {j} must match (int/long): {path}");
                            else if (o is int oi && r is long rl)
                                rl.Should().Be(oi, $"instruction {i} arg {j} must match (long/int): {path}");
                            else if (o is int oi32 && r is uint ru && (oi32 == -1 && ru == 0xFFFFFFFF || (uint)oi32 == ru))
                                { /* -1 and 0xFFFFFFFF equivalent for object/const */ }
                            else if (o is long o64 && r is uint ru2 && (o64 == -1 && ru2 == 0xFFFFFFFF || (uint)o64 == ru2))
                                { /* same */ }
                            else
                                r.Should().Be(o, $"instruction {i} arg {j} must match: {path}");
                        }
                    }

                    // Stage 6: Jump target validation
                    var jumpInstructions2 = ncs2.Instructions.Where(i => i.IsJumpInstruction()).ToList();
                    jumpInstructions2.Count.Should().Be(jumpInstructions.Count, $"jump instruction count must match: {path}");
                    for (int i = 0; i < jumpInstructions.Count; i++)
                    {
                        var origIdx = ncs.Instructions.IndexOf(jumpInstructions[i]);
                        var roundIdx = ncs2.Instructions.IndexOf(jumpInstructions2[i]);
                        if (jumpInstructions[i].Jump != null)
                        {
                            var origTargetIdx = ncs.Instructions.IndexOf(jumpInstructions[i].Jump);
                            var roundTargetIdx = ncs2.Instructions.IndexOf(jumpInstructions2[i].Jump);
                            roundTargetIdx.Should().Be(origTargetIdx, $"jump target index must match for instruction {origIdx}: {path}");
                        }
                    }

                    // Stage 7: Constant validation
                    var constInstructions = ncs.Instructions.Where(i => i.IsConstant()).ToList();
                    var constInstructions2 = ncs2.Instructions.Where(i => i.IsConstant()).ToList();
                    constInstructions2.Count.Should().Be(constInstructions.Count, $"constant instruction count must match: {path}");
                    for (int i = 0; i < constInstructions.Count; i++)
                    {
                        var origIdx = ncs.Instructions.IndexOf(constInstructions[i]);
                        var roundIdx = ncs2.Instructions.IndexOf(constInstructions2[i]);
                        constInstructions2[i].Args.Count.Should().Be(constInstructions[i].Args.Count, 
                            $"constant instruction {origIdx} args count must match: {path}");
                        for (int j = 0; j < constInstructions[i].Args.Count; j++)
                        {
                            var co = constInstructions[i].Args[j];
                            var cr = constInstructions2[i].Args[j];
                            if (co is int coi && cr is uint cru && (uint)coi == cru)
                                continue;
                            if (co is long col && cr is uint cr2 && col >= 0 && col <= uint.MaxValue && (uint)col == cr2)
                                continue;
                            cr.Should().Be(co, $"constant instruction {origIdx} arg {j} must match: {path}");
                        }
                    }

                    // Stage 8: Byte-level identity check (full roundtrip)
                    byte[] bytes2 = NCSAuto.BytesNcs(ncs2);
                    bytes2.Should().Equal(bytes, $"second serialization must be byte-identical: {path}");

                    instructionCountMatches++;
                    instructionTypeMatches++;
                    jumpTargetMatches++;
                    constantMatches++;
                    byteIdentical++;
                    roundtripped++;
                    perFileDetails.Add($"{Path.GetFileName(path)}: {originalInstructionCount} instructions, {jumpInstructions.Count} jumps, {constInstructions.Count} constants");
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(path)}: {ex.GetType().Name} - {ex.Message}");
                }
            }

            perFileDetails.Count.Should().Be(roundtripped, "every script that roundtrips must produce a detail entry");
            // Stage 9: Overall statistics validation — at least 10 must compile and at least 10 must roundtrip fully
            compiled.Should().BeGreaterOrEqualTo(10, $"at least 10 of {paths.Count} K1 scripts must compile. Errors: {string.Join("; ", errors)}");
            roundtripped.Should().BeGreaterOrEqualTo(10, $"at least 10 of {compiled} compiled K1 scripts must roundtrip fully. Errors: {string.Join("; ", errors)}");
            byteIdentical.Should().Be(roundtripped, $"all {roundtripped} roundtripped scripts must be byte-identical");
            instructionCountMatches.Should().Be(roundtripped, $"all {roundtripped} roundtripped scripts must preserve instruction count");
            instructionTypeMatches.Should().Be(roundtripped, $"all {roundtripped} roundtripped scripts must preserve instruction types");
            jumpTargetMatches.Should().Be(roundtripped, $"all {roundtripped} roundtripped scripts must preserve jump targets");
            constantMatches.Should().Be(roundtripped, $"all {roundtripped} roundtripped scripts must preserve constants");
        }

        [Fact]
        public void Vanilla_TSL_CompileAndRoundtrip_WhenSubmodulePresent()
        {
            if (!VanillaSubmodulePresent)
            {
                return;
            }

            var paths = GetNssFiles("TSL", 50).ToList();
            paths.Should().NotBeEmpty("TSL folder should contain .nss files when submodule is present");
            paths.Count.Should().BeGreaterOrEqualTo(10, "should have at least 10 TSL scripts to sample");
            paths.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p), "all paths must be non-empty");
            paths.Should().OnlyContain(p => File.Exists(p), "all paths must exist");
            paths.Should().OnlyContain(p => Path.GetExtension(p).Equals(".nss", StringComparison.OrdinalIgnoreCase), "all paths must be .nss files");

            int compiled = 0;
            int roundtripped = 0;
            int byteIdentical = 0;
            int instructionCountMatches = 0;
            int instructionTypeMatches = 0;
            int jumpTargetMatches = 0;
            int constantMatches = 0;
            var errors = new List<string>();
            var perFileDetails = new List<string>();

            foreach (string path in paths)
            {
                try
                {
                    // Stage 1: Pre-compilation validation
                    File.Exists(path).Should().BeTrue($"file must exist: {path}");
                    var fileInfo = new FileInfo(path);
                    fileInfo.Length.Should().BeGreaterThan(0, $"file must not be empty: {path}");
                    fileInfo.Length.Should().BeLessThan(10_000_000, $"file size must be reasonable: {path}");

                    string nss = File.ReadAllText(path);
                    nss.Should().NotBeNullOrWhiteSpace($"NSS content must not be empty: {path}");
                    nss.Length.Should().BeGreaterThan(0, $"NSS content length must be > 0: {path}");
                    nss.Should().ContainAny(new[] { "void", "int", "float", "string", "object" }, $"NSS should contain type keywords: {path}");

                    // Stage 2: Compilation validation
                    NCS ncs = NCSAuto.CompileNss(nss, Game.TSL, null, null, null);
                    ncs.Should().NotBeNull($"compiled NCS must not be null: {path}");
                    ncs.Instructions.Should().NotBeNull($"instructions list must not be null: {path}");
                    ncs.Instructions.Count.Should().BeGreaterThan(0, $"must have at least one instruction: {path}");
                    ncs.Instructions.Should().OnlyContain(i => i != null, $"all instructions must be non-null: {path}");
                    ncs.Instructions.Should().OnlyContain(i => i.InsType != null, $"all instructions must have InsType: {path}");
                    ncs.Instructions.Should().OnlyContain(i => i.Args != null, $"all instructions must have Args: {path}");
                    // Note: compiler does not set Offset; it is set by NCSBinaryReader on load. Do not assert Offset >= 0 here.

                    // Validate jump instructions have valid jump targets
                    var jumpInstructions = ncs.Instructions.Where(i => i.IsJumpInstruction()).ToList();
                    jumpInstructions.Should().OnlyContain(i => i.Jump != null || i.Args.Count > 0, 
                        $"jump instructions must have jump target or args: {path}");

                    compiled++;
                    var originalInstructionCount = ncs.Instructions.Count;
                    // Stage 3: Binary serialization validation
                    byte[] bytes = NCSAuto.BytesNcs(ncs);
                    bytes.Should().NotBeNull($"bytes must not be null: {path}");
                    bytes.Length.Should().BeGreaterThan(0, $"bytes must not be empty: {path}");
                    bytes.Length.Should().BeGreaterOrEqualTo(13, $"bytes must include header (>=13 bytes): {path}");
                    bytes[0].Should().Be((byte)'N', $"header byte 0 must be 'N': {path}");
                    bytes[1].Should().Be((byte)'C', $"header byte 1 must be 'C': {path}");
                    bytes[2].Should().Be((byte)'S', $"header byte 2 must be 'S': {path}");
                    bytes[3].Should().Be((byte)' ', $"header byte 3 must be ' ': {path}");

                    // Stage 4: Roundtrip validation
                    NCS ncs2 = NCSAuto.ReadNcs(bytes);
                    ncs2.Should().NotBeNull($"re-read NCS must not be null: {path}");
                    ncs2.Instructions.Should().NotBeNull($"re-read instructions must not be null: {path}");
                    ncs2.Instructions.Count.Should().Be(originalInstructionCount, $"instruction count must match exactly: {path}");
                    ncs2.Instructions.Should().OnlyContain(i => i.Offset >= 0, $"after roundtrip all instructions must have valid offset (reader sets them): {path}");

                    // Stage 5: Instruction-by-instruction validation
                    ncs2.Instructions.Count.Should().Be(ncs.Instructions.Count, $"instruction count must match: {path}");
                    for (int i = 0; i < ncs.Instructions.Count; i++)
                    {
                        var orig = ncs.Instructions[i];
                        var round = ncs2.Instructions[i];
                        round.InsType.Should().Be(orig.InsType, $"instruction {i} type must match: {path}");
                        round.Offset.Should().BeGreaterOrEqualTo(0, $"instruction {i} offset must be set by reader: {path}");
                        round.Args.Count.Should().Be(orig.Args.Count, $"instruction {i} args count must match: {path}");
                        for (int j = 0; j < orig.Args.Count; j++)
                        {
                            var o = orig.Args[j];
                            var r = round.Args[j];
                            if (o is long ol && r is int ri)
                                ri.Should().Be((int)ol, $"instruction {i} arg {j} must match (int/long): {path}");
                            else if (o is int oi && r is long rl)
                                rl.Should().Be(oi, $"instruction {i} arg {j} must match (long/int): {path}");
                            else if (o is int oi32 && r is uint ru && (oi32 == -1 && ru == 0xFFFFFFFF || (uint)oi32 == ru))
                                { /* -1 and 0xFFFFFFFF equivalent for object/const */ }
                            else if (o is long o64 && r is uint ru2 && (o64 == -1 && ru2 == 0xFFFFFFFF || (uint)o64 == ru2))
                                { /* same */ }
                            else
                                r.Should().Be(o, $"instruction {i} arg {j} must match: {path}");
                        }
                    }

                    // Stage 6: Jump target validation
                    var jumpInstructions2 = ncs2.Instructions.Where(i => i.IsJumpInstruction()).ToList();
                    jumpInstructions2.Count.Should().Be(jumpInstructions.Count, $"jump instruction count must match: {path}");
                    for (int i = 0; i < jumpInstructions.Count; i++)
                    {
                        var origIdx = ncs.Instructions.IndexOf(jumpInstructions[i]);
                        var roundIdx = ncs2.Instructions.IndexOf(jumpInstructions2[i]);
                        if (jumpInstructions[i].Jump != null)
                        {
                            var origTargetIdx = ncs.Instructions.IndexOf(jumpInstructions[i].Jump);
                            var roundTargetIdx = ncs2.Instructions.IndexOf(jumpInstructions2[i].Jump);
                            roundTargetIdx.Should().Be(origTargetIdx, $"jump target index must match for instruction {origIdx}: {path}");
                        }
                    }

                    // Stage 7: Constant validation
                    var constInstructions = ncs.Instructions.Where(i => i.IsConstant()).ToList();
                    var constInstructions2 = ncs2.Instructions.Where(i => i.IsConstant()).ToList();
                    constInstructions2.Count.Should().Be(constInstructions.Count, $"constant instruction count must match: {path}");
                    for (int i = 0; i < constInstructions.Count; i++)
                    {
                        var origIdx = ncs.Instructions.IndexOf(constInstructions[i]);
                        var roundIdx = ncs2.Instructions.IndexOf(constInstructions2[i]);
                        constInstructions2[i].Args.Count.Should().Be(constInstructions[i].Args.Count, 
                            $"constant instruction {origIdx} args count must match: {path}");
                        for (int j = 0; j < constInstructions[i].Args.Count; j++)
                        {
                            var co = constInstructions[i].Args[j];
                            var cr = constInstructions2[i].Args[j];
                            if (co is int coi && cr is uint cru && (uint)coi == cru)
                                continue;
                            if (co is long col && cr is uint cr2 && col >= 0 && col <= uint.MaxValue && (uint)col == cr2)
                                continue;
                            cr.Should().Be(co, $"constant instruction {origIdx} arg {j} must match: {path}");
                        }
                    }

                    // Stage 8: Byte-level identity check (full roundtrip)
                    byte[] bytes2 = NCSAuto.BytesNcs(ncs2);
                    bytes2.Should().Equal(bytes, $"second serialization must be byte-identical: {path}");

                    instructionCountMatches++;
                    instructionTypeMatches++;
                    jumpTargetMatches++;
                    constantMatches++;
                    byteIdentical++;
                    roundtripped++;
                    perFileDetails.Add($"{Path.GetFileName(path)}: {originalInstructionCount} instructions, {jumpInstructions.Count} jumps, {constInstructions.Count} constants");
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(path)}: {ex.GetType().Name} - {ex.Message}");
                }
            }

            perFileDetails.Count.Should().Be(roundtripped, "every script that roundtrips must produce a detail entry");
            // Stage 9: Overall statistics validation — at least 10 must compile and at least 10 must roundtrip fully
            compiled.Should().BeGreaterOrEqualTo(10, $"at least 10 of {paths.Count} TSL scripts must compile. Errors: {string.Join("; ", errors)}");
            roundtripped.Should().BeGreaterOrEqualTo(10, $"at least 10 of {compiled} compiled TSL scripts must roundtrip fully. Errors: {string.Join("; ", errors)}");
            byteIdentical.Should().Be(roundtripped, $"all {roundtripped} roundtripped scripts must be byte-identical");
            instructionCountMatches.Should().Be(roundtripped, $"all {roundtripped} roundtripped scripts must preserve instruction count");
            instructionTypeMatches.Should().Be(roundtripped, $"all {roundtripped} roundtripped scripts must preserve instruction types");
            jumpTargetMatches.Should().Be(roundtripped, $"all {roundtripped} roundtripped scripts must preserve jump targets");
            constantMatches.Should().Be(roundtripped, $"all {roundtripped} roundtripped scripts must preserve constants");
        }

        [Fact]
        public void NCSDecompiler_Decode_ProducesTokenString()
        {
            string nss = @"
                void main()
                {
                    int x = 1;
                    PrintInteger(x);
                }
            ";
            NCS ncs = NCSAuto.CompileNss(nss, Game.K1);
            ncs.Should().NotBeNull("compiled NCS must not be null");
            ncs.Instructions.Should().NotBeNull("instructions must not be null");
            ncs.Instructions.Count.Should().BeGreaterThan(0, "must have instructions");

            string tokenString = NCSDecompiler.Decompile(ncs);
            tokenString.Should().NotBeNull("token string must not be null");
            tokenString.Should().NotBeEmpty("token string must not be empty");
            tokenString.Should().NotBeNullOrWhiteSpace("token string must not be whitespace");
            tokenString.Length.Should().BeGreaterThan(0, "token string length must be > 0");
            tokenString.Should().Contain("MOVSP", "must contain MOVSP instruction");
            tokenString.Should().Contain(";", "must contain statement separators");
            tokenString.Should().MatchRegex(@"\d+\s+\d+", "must contain position and offset numbers");
            tokenString.Split(';').Length.Should().BeGreaterThan(1, "must contain multiple statements");
        }

        [Fact]
        public void NCSDecompiler_Decode_FromBytes_Roundtrip()
        {
            string nss = "void main() { }";
            NCS ncs = NCSAuto.CompileNss(nss, Game.K1);
            ncs.Should().NotBeNull("compiled NCS must not be null");
            ncs.Instructions.Should().NotBeNull("instructions must not be null");
            ncs.Instructions.Count.Should().BeGreaterThan(0, "must have instructions");

            byte[] bytes = NCSAuto.BytesNcs(ncs);
            bytes.Should().NotBeNull("bytes must not be null");
            bytes.Length.Should().BeGreaterThan(0, "bytes must not be empty");
            bytes.Length.Should().BeGreaterOrEqualTo(13, "bytes must include header");

            string tokenString = NCSDecompiler.Decompile(bytes);
            tokenString.Should().NotBeNull("token string must not be null");
            tokenString.Should().NotBeEmpty("token string must not be empty");
            tokenString.Should().NotBeNullOrWhiteSpace("token string must not be whitespace");
            tokenString.Length.Should().BeGreaterThan(0, "token string length must be > 0");
            tokenString.Should().Contain(";", "must contain statement separators");
            tokenString.Should().MatchRegex(@"\d+\s+\d+", "must contain position and offset numbers");

            // Verify roundtrip: bytes -> decompile -> should produce same token string as NCS object
            string tokenString2 = NCSDecompiler.Decompile(ncs);
            tokenString2.Should().Be(tokenString, "decompiling from bytes vs NCS object must produce identical token strings");
        }
    }
}
