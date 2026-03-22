// Copyright 2021-2025 DeNCS / KPatcher.Tests contributors
// Licensed under the MIT License. See licenses in the repository for full license text.
//
// C# port of vendor/DeNCS/src/test/java/.../DeNCSCLIRoundTripTest.java
// for KPatcher.Tests (xUnit). Behavior matches the Java original: external nwnnsscomp,
// managed NCSDecomp.Core decompile, Java-equivalent text normalization (must match after normalize),
// recompile with nwnnsscomp (required), raw bytecode equality vs original .ncs.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.NCS;
using NCSDecomp.Core;
using NCSDecomp.Core.Utils;
using Xunit;

namespace KPatcher.Core.Tests.Formats
{
    /// <summary>
    /// Exhaustive round-trip tests aligned with DeNCS Java <c>DeNCSCLIRoundTripTest</c>
    /// (<c>vendor/DeNCS/src/test/java/com/kotor/resource/formats/ncs/DeNCSCLIRoundTripTest.java</c>).
    /// xUnit <c>DisplayName</c> matches JUnit method names. Requires git on PATH to clone vanilla scripts if
    /// <c>test-work/Vanilla_KOTOR_Script_Source</c> is absent. <c>nwnnsscomp</c> is resolved via
    /// <see cref="CompilerUtil.ResolveCompilerPathWithFallbacks"/> (includes <c>vendor/DeNCS/tools</c>). Override with <c>NWNNSCOMP_PATH</c> if needed.
    /// </summary>
    /// <remarks>
    /// <para><b>CRITICAL: TEST PHILOSOPHY</b> (same intent as the Java source; do not “fix” decompiler output here.)</para>
    /// <list type="bullet">
    /// <item>These tests validate the decompiler against original scripts. They do NOT mask or patch decompiler flaws.</item>
    /// <item><b>Forbidden:</b> fixing syntax/logic in decompiled output; patching mangled code; editing expressions, operators, semicolons, braces, types, returns; adjusting signatures for correctness; any output “repair”.</item>
    /// <item><b>Allowed (comparison only):</b> the <c>NormalizeNewlines</c> pipeline (whitespace, comments, includes, placeholder/globals, variable renames, brace layout, etc.) for text comparison — does not legitimize patching broken decompiler output.</item>
    /// <item>After that normalization, original vs decompiled NSS must match exactly or the case fails (same for external vs KCompiler decompiled NSS).</item>
    /// <item>Bugs belong in the decompiler implementation, not in this harness.</item>
    /// </list>
    /// <para><b>Intentional KPatcher / .NET divergences from stock JUnit runs:</b></para>
    /// <list type="number">
    /// <item><b>Opt-in suite:</b> set <c>RUN_NCSDECOMP_JAVA_ROUNDTRIP_SUITE=1</c> so default <c>dotnet test</c> stays fast; upstream JUnit runs the suite every <c>mvn test</c>.</item>
    /// <item><b>KCompiler parity:</b> each <c>RoundTripSingle</c> run also compiles the same NSS with <see cref="NCSAuto.CompileNss"/>, asserts bytecode/pcode parity vs <c>nwnnsscomp</c> output (<c>AssertBytecodeEqual</c>), decompiles the managed <c>.ncs</c>, and asserts normalized NSS matches the decompilation of the external <c>.ncs</c>.</item>
    /// <item><b>Repo root:</b> walk up to <c>KPatcher.sln</c> from <see cref="AppContext.BaseDirectory"/> (Java used the DeNCS Maven module directory as cwd).</item>
    /// <item><b>Nwscript paths:</b> <c>include/k1_nwscript.nss</c>, <c>include/k2_nwscript.nss</c>, and <c>vendor/DeNCS/tools/k1_asc_donotuse_nwscript.nss</c> (with fallbacks), vs Java <c>src/main/resources</c> + <c>tools</c>.</item>
    /// <item><b>Decompile step:</b> <see cref="RoundTripUtil.DecompileNcsToNssFile"/> (managed NCS decompiler pipeline) instead of spawning the Java <c>DeNCSCLI</c> process; inputs/outputs are file-level equivalent.</item>
    /// </list>
    /// <para>Java’s <c>*_REMOVED</c> legacy bodies and <c>loadNwscriptSignatures</c> helpers appear later in this file (uncalled, matching Java). The three guard stubs that throw appear in the same source.</para>
    /// <para>CLI entry parity: <see cref="RunMain"/> supports <c>--no-resume</c>, <c>--max-seconds</c>, <c>--save-progress-every</c>, and optional single-file + game, like Java <c>main</c>.</para>
    /// </remarks>
    [Trait("Category", "DeNCSJavaParity")]
    [Trait("Category", "ExternalCompiler")]
    [Trait("Category", "Vendor")]
    public sealed class NCSDecompCliRoundTripTest
    {
        private const string RunSuiteEnv = "RUN_NCSDECOMP_JAVA_ROUNDTRIP_SUITE";

        private long _maxSuiteNanos;
        private int _saveProgressEvery = 200;

        private static bool ShouldRunSuite()
        {
            return string.Equals(Environment.GetEnvironmentVariable(RunSuiteEnv), "1", StringComparison.Ordinal);
        }

        private static Game GameFlagToGame(string gameFlag)
        {
            return string.Equals(gameFlag, "k2", StringComparison.Ordinal) ? Game.TSL : Game.K1;
        }

        /// <summary>Java <c>main</c> equivalent for manual / debugger runs.</summary>
        public static int RunMain(string[] args)
        {
            var runner = new NCSDecompCliRoundTripTest();
            bool useResume = true;
            var positional = new List<string>();
            int? maxSeconds = null;
            int? saveEvery = null;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (string.Equals(a, "--no-resume", StringComparison.Ordinal))
                {
                    useResume = false;
                    continue;
                }

                if (string.Equals(a, "--max-seconds", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    maxSeconds = int.Parse(args[++i]);
                    continue;
                }

                if (string.Equals(a, "--save-progress-every", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    saveEvery = int.Parse(args[++i]);
                    continue;
                }

                positional.Add(a);
            }

            if (maxSeconds.HasValue && maxSeconds.Value > 0)
            {
                runner._maxSuiteNanos = (long)maxSeconds.Value * 1_000_000_000L;
            }

            if (saveEvery.HasValue)
            {
                runner._saveProgressEvery = Math.Max(0, saveEvery.Value);
            }

            if (positional.Count > 0)
            {
                string filename = positional[0];
                string gameFlag = positional.Count > 1 ? positional[1] : "k1";
                return runner.TestSingleFile(filename, gameFlag);
            }

            return runner.RunRoundTripSuite(useResume);
        }

        /// <summary>JUnit <c>testRoundTripSuite</c> equivalent (gated by <see cref="RunSuiteEnv"/> for default CI).</summary>
        [Fact(DisplayName = "DeNCSCLIRoundTripTest.testRoundTripSuite")]
        public void TestRoundTripSuite_JavaParity()
        {
            if (!ShouldRunSuite())
            {
                return;
            }

            var runner = new NCSDecompCliRoundTripTest();
            Assert.Equal(0, runner.RunRoundTripSuite(false));
        }

        /// <summary>JUnit <c>testRoundTripBytecodeSuite</c> equivalent (gated by <see cref="RunSuiteEnv"/> for default CI).</summary>
        [Fact(DisplayName = "DeNCSCLIRoundTripTest.testRoundTripBytecodeSuite")]
        public void TestRoundTripBytecodeSuite_JavaParity()
        {
            if (!ShouldRunSuite())
            {
                return;
            }

            var runner = new NCSDecompCliRoundTripTest();
            Assert.Equal(0, runner.RunRoundTripBytecodeSuite(false));
        }

        private static string ResolveRepoRoot()
        {
            string baseDir = AppContext.BaseDirectory;
            for (var dir = baseDir; !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir))
            {
                if (File.Exists(Path.Combine(dir, "KPatcher.sln")))
                {
                    return dir;
                }
            }

            return Directory.GetCurrentDirectory();
        }

        private static readonly string RepoRoot = Path.GetFullPath(ResolveRepoRoot());
        private static readonly string TestWorkDir = Path.Combine(RepoRoot, "test-work");
        private static readonly string VanillaRepoDir = Path.Combine(TestWorkDir, "Vanilla_KOTOR_Script_Source");
        private static readonly string ResumeFile = Path.Combine(TestWorkDir, ".test-resume");

        private static readonly string[] VanillaRepoUrls =
        {
            "https://github.com/KOTORCommunityPatches/Vanilla_KOTOR_Script_Source.git",
            "https://github.com/th3w1zard1/Vanilla_KOTOR_Script_Source.git"
        };

        private static string FindCompiler()
        {
            string path = CompilerUtil.ResolveCompilerPathWithFallbacks(Environment.GetEnvironmentVariable("NWNNSCOMP_PATH"));
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                return path;
            }

            // Explicit repo-root vendor layout (static init runs before any walk from host dirs may differ).
            string vendorTools = Path.Combine(RepoRoot, "vendor", "DeNCS", "tools");
            if (Directory.Exists(vendorTools))
            {
                foreach (string name in CompilerUtil.CompilerNames)
                {
                    string candidate = Path.Combine(vendorTools, name);
                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
            }

            return Path.Combine(RepoRoot, "tools", "nwnnsscomp.exe");
        }

        private static readonly string NwnCompiler = FindCompiler();

        private static readonly string K1Nwscript = Path.Combine(RepoRoot, "include", "k1_nwscript.nss");
        private static readonly string K1AscNwscript = ResolveK1AscNwscriptPath();

        private static string ResolveK1AscNwscriptPath()
        {
            return FirstExistingPath(
                Path.Combine(RepoRoot, "vendor", "DeNCS", "tools", "k1_asc_donotuse_nwscript.nss"),
                CompilerUtil.ResolveToolsFile("k1_asc_donotuse_nwscript.nss"));
        }

        private static readonly string K2Nwscript = Path.Combine(RepoRoot, "include", "k2_nwscript.nss");

        private static string FirstExistingPath(params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (!string.IsNullOrEmpty(paths[i]) && File.Exists(paths[i]))
                {
                    return Path.GetFullPath(paths[i]);
                }
            }

            return null;
        }

        private static readonly Dictionary<string, string> NpcConstantsK1 = LoadConstantsWithPrefix(K1Nwscript, "NPC_");
        private static readonly Dictionary<string, string> NpcConstantsK2 = LoadConstantsWithPrefix(K2Nwscript, "NPC_");
        private static readonly Dictionary<string, string> AbilityConstantsK1 = LoadConstantsWithPrefix(K1Nwscript, "ABILITY_");
        private static readonly Dictionary<string, string> AbilityConstantsK2 = LoadConstantsWithPrefix(K2Nwscript, "ABILITY_");
        private static readonly Dictionary<string, string> FactionConstantsK1 = LoadConstantsWithPrefix(K1Nwscript, "STANDARD_FACTION_");
        private static readonly Dictionary<string, string> FactionConstantsK2 = LoadConstantsWithPrefix(K2Nwscript, "STANDARD_FACTION_");
        private static readonly Dictionary<string, string> AnimationConstantsK1 = LoadConstantsWithPrefix(K1Nwscript, "ANIMATION_");
        private static readonly Dictionary<string, string> AnimationConstantsK2 = LoadConstantsWithPrefix(K2Nwscript, "ANIMATION_");

        private static string DisplayPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            string absStr = Path.GetFullPath(path).Replace('\\', '/');

            var candidates = new List<string> { absStr };
            AddRelIfPossible(candidates, RepoRoot, path);
            AddRelIfPossible(candidates, TestWorkDir, path);
            AddRelIfPossible(candidates, VanillaRepoDir, path);

            string best = candidates
                .Where(s => !string.IsNullOrEmpty(s))
                .OrderBy(s => s.Length)
                .FirstOrDefault() ?? absStr;

            return string.Equals(best, ".", StringComparison.Ordinal) ? best : best.Replace('\\', '/');
        }

        private static void AddRelIfPossible(List<string> candidates, string basePath, string targetPath)
        {
            try
            {
                string rel = Path.GetRelativePath(Path.GetFullPath(basePath), Path.GetFullPath(targetPath));
                rel = rel.Replace('\\', '/');
                candidates.Add(string.IsNullOrEmpty(rel) ? "." : rel);
            }
            catch (ArgumentException)
            {
                // different roots
            }
        }

        private static void CopyWithRetry(string source, string target)
        {
            const int attempts = 3;
            for (int i = 1; i <= attempts; i++)
            {
                try
                {
                    string parent = Path.GetDirectoryName(target);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        Directory.CreateDirectory(parent);
                    }

                    File.Copy(source, target, true);
                    return;
                }
                catch (IOException)
                {
                    if (i == attempts)
                    {
                        throw;
                    }

                    Thread.Sleep(200 * i);
                }
            }
        }

        /// <summary>Java <c>Files.isSameFile</c> subset: true when both paths resolve to the same full path (e.g. junction to same file). Different paths always false so nwscript is refreshed like Java when dest is under <c>tools/</c>.</summary>
        private static bool IsSameFile(string pathA, string pathB)
        {
            try
            {
                if (string.IsNullOrEmpty(pathA) || string.IsNullOrEmpty(pathB))
                {
                    return false;
                }

                string fa = Path.GetFullPath(pathA);
                string fb = Path.GetFullPath(pathB);
                return string.Equals(fa, fb, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static readonly string WorkRoot = Path.Combine(TestWorkDir, "roundtrip-work");
        private static readonly string ProfileOutput = Path.Combine(TestWorkDir, "test_profile.txt");
        private static readonly string CompileTempRoot = Path.Combine(TestWorkDir, "compile-temp");
        private static readonly TimeSpan ProcTimeout = TimeSpan.FromSeconds(25);

        private static volatile string _stagedNwscriptGameFlag;
        private static readonly ConcurrentDictionary<string, byte> StagedIncludes = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, long> OperationTimes = new Dictionary<string, long>();
        private static long _testStartTime;
        private static int _totalTests;
        private static int _testsProcessed;

        private static string _k1Scratch;
        private static string _k2Scratch;

        private static void ResetPerformanceTracking()
        {
            lock (OperationTimes)
            {
                OperationTimes.Clear();
            }

            _testsProcessed = 0;
            _totalTests = 0;
        }

        private static void EnsureVanillaRepo()
        {
            if (Directory.Exists(VanillaRepoDir) && IsGitRepo(VanillaRepoDir))
            {
                Console.WriteLine("Using existing Vanilla_KOTOR_Script_Source repository at: " + DisplayPath(VanillaRepoDir));
                return;
            }

            if (Directory.Exists(VanillaRepoDir))
            {
                Console.WriteLine("Removing non-git directory: " + DisplayPath(VanillaRepoDir));
                DeleteDirectory(VanillaRepoDir);
            }

            Console.WriteLine("Cloning Vanilla_KOTOR_Script_Source repository...");
            Console.WriteLine("  Destination: " + DisplayPath(VanillaRepoDir));

            Directory.CreateDirectory(Path.GetDirectoryName(VanillaRepoDir) ?? TestWorkDir);

            IOException lastException = null;
            for (int i = 0; i < VanillaRepoUrls.Length; i++)
            {
                string repoUrl = VanillaRepoUrls[i];
                Console.WriteLine("  Attempting URL " + (i + 1) + "/" + VanillaRepoUrls.Length + ": " + repoUrl);

                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add("clone");
                psi.ArgumentList.Add(repoUrl);
                psi.ArgumentList.Add(VanillaRepoDir);

                var output = new StringBuilder();
                using (var proc = Process.Start(psi))
                {
                    if (proc == null)
                    {
                        lastException = new IOException("Failed to start git");
                        continue;
                    }

                    string merged = ReadProcessStreams(proc);
                    output.Append(merged);
                    proc.WaitForExit();
                    if (proc.ExitCode == 0)
                    {
                        Console.WriteLine("Repository cloned successfully from: " + repoUrl);
                        return;
                    }

                    lastException = new IOException("Failed to clone from " + repoUrl + ". Exit code: " + proc.ExitCode + "\nOutput: " + output);
                    Console.WriteLine("  Failed: " + lastException.Message.Split('\n')[0]);
                    if (Directory.Exists(VanillaRepoDir))
                    {
                        try
                        {
                            DeleteDirectory(VanillaRepoDir);
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                }
            }

            throw new IOException("Failed to clone repository from all URLs.\nLast error: " + (lastException != null ? lastException.Message : "Unknown"));
        }

        private static bool IsGitRepo(string dir)
        {
            string gitDir = Path.Combine(dir, ".git");
            return Directory.Exists(gitDir) || File.Exists(gitDir);
        }

        private static void Preflight()
        {
            Console.WriteLine("=== Preflight Checks ===");

            EnsureVanillaRepo();

            if (!File.Exists(NwnCompiler))
            {
                throw new IOException("nwnnsscomp.exe missing at: " + DisplayPath(NwnCompiler));
            }

            Console.WriteLine("✓ Found compiler: " + DisplayPath(NwnCompiler));

            if (!File.Exists(K1Nwscript))
            {
                throw new IOException("k1_nwscript.nss missing at: " + DisplayPath(K1Nwscript));
            }

            Console.WriteLine("✓ Found K1 nwscript: " + DisplayPath(K1Nwscript));

            if (string.IsNullOrEmpty(K1AscNwscript) || !File.Exists(K1AscNwscript))
            {
                throw new IOException(
                    "k1_asc_donotuse_nwscript.nss missing (expected vendor/DeNCS/tools or tools/ next to nwnnsscomp). " +
                    "Without it, scripts using 11-arg ActionStartConversation cannot match Java preflight.");
            }

            Console.WriteLine("✓ Found K1 ASC nwscript: " + DisplayPath(K1AscNwscript));

            if (!File.Exists(K2Nwscript))
            {
                // Match Java preflight wording (repo uses include/k2_nwscript.nss as TSL nwscript).
                throw new IOException("tsl_nwscript.nss missing at: " + DisplayPath(K2Nwscript));
            }

            Console.WriteLine("✓ Found TSL nwscript: " + DisplayPath(K2Nwscript));

            string k1Root = Path.Combine(VanillaRepoDir, "K1");
            string tslRoot = Path.Combine(VanillaRepoDir, "TSL");
            if (!Directory.Exists(k1Root))
            {
                throw new IOException("K1 directory not found in vanilla repo: " + DisplayPath(k1Root));
            }

            if (!Directory.Exists(tslRoot))
            {
                throw new IOException("TSL directory not found in vanilla repo: " + DisplayPath(tslRoot));
            }

            Console.WriteLine("✓ Vanilla repo structure verified");

            _k1Scratch = PrepareScratch("k1", K1Nwscript);
            _k2Scratch = PrepareScratch("k2", K2Nwscript);

            string cwd = Directory.GetCurrentDirectory();
            string toolsDir = Path.Combine(cwd, "tools");
            Directory.CreateDirectory(toolsDir);
            string k1NwTools = Path.Combine(toolsDir, "k1_nwscript.nss");
            string k2NwTools = Path.Combine(toolsDir, "tsl_nwscript.nss");

            try
            {
                if (!File.Exists(k1NwTools) || !IsSameFile(K1Nwscript, k1NwTools))
                {
                    CopyWithRetry(K1Nwscript, k1NwTools);
                }
            }
            catch (IOException ex)
            {
                if (!File.Exists(k1NwTools))
                {
                    throw;
                }

                Console.WriteLine("! Warning: could not refresh k1_nwscript.nss (" + ex.Message + "); using existing copy at " + DisplayPath(k1NwTools));
            }

            try
            {
                if (!File.Exists(k2NwTools) || !IsSameFile(K2Nwscript, k2NwTools))
                {
                    CopyWithRetry(K2Nwscript, k2NwTools);
                }
            }
            catch (IOException ex)
            {
                if (!File.Exists(k2NwTools))
                {
                    throw;
                }

                Console.WriteLine("! Warning: could not refresh tsl_nwscript.nss (" + ex.Message + "); using existing copy at " + DisplayPath(k2NwTools));
            }

            Console.WriteLine("=== Preflight Complete ===\n");
        }

        private List<RoundTripCase> BuildRoundTripCases()
        {
            Console.WriteLine("=== Discovering Test Files ===");

            var allFiles = new List<TestItem>();

            string k1Root = Path.Combine(VanillaRepoDir, "K1");
            if (Directory.Exists(k1Root))
            {
                foreach (string p in Directory.EnumerateFiles(k1Root, "*.nss", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    allFiles.Add(new TestItem(p, "k1", _k1Scratch));
                }
            }

            string tslVanilla = Path.Combine(VanillaRepoDir, "TSL", "Vanilla");
            if (Directory.Exists(tslVanilla))
            {
                foreach (string p in Directory.EnumerateFiles(tslVanilla, "*.nss", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    allFiles.Add(new TestItem(p, "k2", _k2Scratch));
                }
            }

            string tslTslrcm = Path.Combine(VanillaRepoDir, "TSL", "TSLRCM");
            if (Directory.Exists(tslTslrcm))
            {
                foreach (string p in Directory.EnumerateFiles(tslTslrcm, "*.nss", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    allFiles.Add(new TestItem(p, "k2", _k2Scratch));
                }
            }

            Console.WriteLine("Found " + allFiles.Count + " .nss files");

            allFiles.Sort((a, b) =>
            {
                int gameCompare = string.CompareOrdinal(a.GameFlag, b.GameFlag);
                if (gameCompare != 0)
                {
                    return gameCompare;
                }

                return string.CompareOrdinal(a.Path, b.Path);
            });

            var tests = new List<RoundTripCase>();
            foreach (TestItem item in allFiles)
            {
                string relPath = Path.GetRelativePath(VanillaRepoDir, item.Path);
                string displayName = string.Equals(item.GameFlag, "k1", StringComparison.Ordinal)
                    ? "K1: " + relPath
                    : "TSL: " + relPath;
                tests.Add(new RoundTripCase(displayName, item));
            }

            _totalTests = tests.Count;
            Console.WriteLine("=== Test Discovery Complete ===\n");
            return tests;
        }

        private sealed class TestItem
        {
            public TestItem(string path, string gameFlag, string scratchRoot)
            {
                Path = path;
                GameFlag = gameFlag;
                ScratchRoot = scratchRoot;
            }

            public string Path { get; }
            public string GameFlag { get; }
            public string ScratchRoot { get; }
        }

        private sealed class RoundTripCase
        {
            public RoundTripCase(string displayName, TestItem item)
            {
                DisplayName = displayName;
                Item = item;
            }

            public string DisplayName { get; }
            public TestItem Item { get; }
        }

        private sealed class RoundTripResult
        {
            public RoundTripResult(string capturedOutput, Exception exception)
            {
                CapturedOutput = capturedOutput;
                Exception = exception;
            }

            public string CapturedOutput { get; }
            public Exception Exception { get; }
        }

        private static RoundTripResult RoundTripSingleWithOutputCapture(string nssPath, string gameFlag, string scratchRoot)
        {
            var outCapture = new StringWriter();
            var errCapture = new StringWriter();
            TextWriter originalOut = Console.Out;
            TextWriter originalErr = Console.Error;
            try
            {
                Console.SetOut(outCapture);
                Console.SetError(errCapture);
                RoundTripSingle(nssPath, gameFlag, scratchRoot);
                string captured = outCapture.ToString() + errCapture.ToString();
                return new RoundTripResult(captured, null);
            }
            catch (Exception e)
            {
                string captured = outCapture.ToString() + errCapture.ToString();
                return new RoundTripResult(captured, e);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
            }
        }

        private static string Sha256HexFile(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static void AppendFileDiagnosticBlock(StringBuilder sb, string title, string path, bool includeHash)
        {
            sb.Append("--- ").Append(title).Append(" ---\n");
            sb.Append("Path: ").Append(DisplayPath(path)).Append('\n');
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                sb.Append("Status: MISSING (expected file not found on disk)\n\n");
                return;
            }

            try
            {
                var fi = new FileInfo(path);
                sb.Append("Status: present\n");
                sb.Append("Size: ").Append(fi.Length).Append(" bytes\n");
                if (includeHash && fi.Length <= 50_000_000)
                {
                    sb.Append("SHA256: ").Append(Sha256HexFile(path)).Append('\n');
                }
                else if (includeHash)
                {
                    sb.Append("SHA256: (skipped — file larger than 50 MB)\n");
                }
            }
            catch (Exception ex)
            {
                sb.Append("Could not stat file: ").Append(ex.Message).Append('\n');
            }

            sb.Append('\n');
        }

        private static void AppendNssSnippet(StringBuilder sb, string label, string path, int maxLines)
        {
            sb.Append("-- ").Append(label).Append(" (first ").Append(maxLines).Append(" non-empty lines) --\n");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                sb.Append("(unavailable)\n\n");
                return;
            }

            try
            {
                int n = 0;
                foreach (string line in File.ReadLines(path, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    n++;
                    sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0,4}| {1}\n", n, line);
                    if (n >= maxLines)
                    {
                        break;
                    }
                }

                if (n == 0)
                {
                    sb.Append("(file empty or whitespace only)\n");
                }
            }
            catch (Exception ex)
            {
                sb.Append("Could not read: ").Append(ex.Message).Append('\n');
            }

            sb.Append('\n');
        }

        private static void RequireRoundTripOutputFile(string path, string artifactDescription, string nssPath, string gameFlag, string scratchRoot)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new IOException(
                    "Round-trip invariant violated: output path for \"" + artifactDescription + "\" is null or empty.\n" +
                    "Source NSS: " + DisplayPath(nssPath) + "\n" +
                    "Game: " + gameFlag + "\n" +
                    "Scratch: " + DisplayPath(scratchRoot));
            }

            if (!File.Exists(path))
            {
                var sb = new StringBuilder();
                sb.Append("Round-trip invariant violated: expected output file was not produced.\n");
                sb.Append("Artifact: ").Append(artifactDescription).Append('\n');
                sb.Append("Expected path: ").Append(DisplayPath(path)).Append('\n');
                sb.Append("Source NSS: ").Append(DisplayPath(nssPath)).Append('\n');
                sb.Append("Game flag: ").Append(gameFlag).Append('\n');
                sb.Append("Scratch root: ").Append(DisplayPath(scratchRoot)).Append('\n');
                sb.Append("Check prior steps in the log: compiler/decompiler may have failed silently or wrote elsewhere.\n");
                throw new IOException(sb.ToString());
            }

            try
            {
                long len = new FileInfo(path).Length;
                if (len <= 0)
                {
                    throw new IOException(
                        "Round-trip invariant violated: \"" + artifactDescription + "\" exists but is empty (0 bytes).\n" +
                        "Path: " + DisplayPath(path));
                }
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException("Could not verify size of \"" + DisplayPath(path) + "\": " + ex.Message, ex);
            }
        }

        private static void AppendStringMismatchDetails(StringBuilder sb, string a, string b, string contextLabel)
        {
            sb.Append("=== ").Append(contextLabel).Append(" — detailed comparison ===\n");
            sb.Append("Normalized A length: ").Append(a?.Length ?? 0).Append(" chars\n");
            sb.Append("Normalized B length: ").Append(b?.Length ?? 0).Append(" chars\n");
            if (a == null)
            {
                a = string.Empty;
            }

            if (b == null)
            {
                b = string.Empty;
            }

            int max = Math.Max(a.Length, b.Length);
            int firstDiff = -1;
            for (int i = 0; i < max; i++)
            {
                char ca = i < a.Length ? a[i] : '\0';
                char cb = i < b.Length ? b[i] : '\0';
                if (ca != cb)
                {
                    firstDiff = i;
                    break;
                }
            }

            if (firstDiff < 0)
            {
                sb.Append("(Strings compare equal — this block should not appear.)\n\n");
                return;
            }

            sb.Append("First differing character index: ").Append(firstDiff).Append(" (0x")
                .Append(firstDiff.ToString("x", System.Globalization.CultureInfo.InvariantCulture)).Append(")\n");
            int lineA = 1;
            int colA = 1;
            for (int i = 0; i < firstDiff && i < a.Length; i++)
            {
                if (a[i] == '\n')
                {
                    lineA++;
                    colA = 1;
                }
                else
                {
                    colA++;
                }
            }

            sb.Append("Approximate position in A: line ").Append(lineA).Append(", column ").Append(colA).Append('\n');
            char ac = firstDiff < a.Length ? a[firstDiff] : '\0';
            char bc = firstDiff < b.Length ? b[firstDiff] : '\0';
            sb.Append("At index: A has ");
            if (firstDiff >= a.Length)
            {
                sb.Append("<EOF>");
            }
            else
            {
                sb.Append('\'').Append(ac).Append("' U+").Append(((int)ac).ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
            }

            sb.Append(" | B has ");
            if (firstDiff >= b.Length)
            {
                sb.Append("<EOF>");
            }
            else
            {
                sb.Append('\'').Append(bc).Append("' U+").Append(((int)bc).ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
            }

            sb.Append("\n\n-- Context window around first diff (A) --\n");
            AppendTextWindow(sb, a, firstDiff, 240);
            sb.Append("\n-- Context window around first diff (B) --\n");
            AppendTextWindow(sb, b, firstDiff, 240);
            sb.Append('\n');
        }

        private static void AppendTextWindow(StringBuilder sb, string s, int focusIndex, int radius)
        {
            if (string.IsNullOrEmpty(s))
            {
                sb.Append("(empty string)\n");
                return;
            }

            int start = Math.Max(0, focusIndex - radius);
            int end = Math.Min(s.Length, focusIndex + radius);
            string window = s.Substring(start, end - start);
            sb.Append("Window [").Append(start).Append("..").Append(end).Append(") relative to full string:\n");
            sb.Append(window.Replace("\r", "\\r").Replace("\n", "\\n\n"));
            sb.Append('\n');
        }

        private static string BuildNwnnsscompOriginalCompileFailureMessage(
            string nssPath,
            string gameFlag,
            string expectedNcsOut,
            string displayRelPath,
            Exception cause)
        {
            var sb = new StringBuilder();
            sb.Append("═══════════════════════════════════════════════════════════════\n");
            sb.Append("ORIGINAL NSS FAILED TO COMPILE WITH nwnnsscomp.exe\n");
            sb.Append("═══════════════════════════════════════════════════════════════\n\n");
            sb.Append("This is treated as a test failure (no skipping). The vanilla source must compile with the same\n");
            sb.Append("legacy compiler the round-trip harness uses, or the case cannot validate decompiler behavior.\n\n");
            sb.Append("WHAT TO CHECK:\n");
            sb.Append("  • Staging paths and #include resolution under scratch (see RunCompiler logs above).\n");
            sb.Append("  • Correct game flag (k1 vs k2) and nwscript.nss / k1_asc copy in compiler directory.\n");
            sb.Append("  • NWNNSCOMP_PATH if you override the compiler; vendor/DeNCS/tools fallback.\n\n");
            sb.Append("Script (relative to vanilla repo): ").Append(displayRelPath).Append('\n');
            AppendFileDiagnosticBlock(sb, "Source .nss", nssPath, includeHash: true);
            AppendFileDiagnosticBlock(sb, "Expected .ncs output (may be absent)", expectedNcsOut, includeHash: false);
            AppendNssSnippet(sb, "Source NSS snippet", nssPath, 40);
            sb.Append("-- Compiler / tool exception --\n");
            sb.Append(cause.ToString()).Append("\n\n");
            return sb.ToString();
        }

        private static string BuildKCompilerCompileFailureMessage(string nssPath, string gameFlag, Exception cause)
        {
            var sb = new StringBuilder();
            sb.Append("═══════════════════════════════════════════════════════════════\n");
            sb.Append("KCompiler (managed) FAILED TO COMPILE THE SAME NSS\n");
            sb.Append("═══════════════════════════════════════════════════════════════\n\n");
            sb.Append("The exhaustive round-trip requires the managed compiler to accept the same source as nwnnsscomp.\n\n");
            sb.Append("Game: ").Append(gameFlag).Append('\n');
            AppendFileDiagnosticBlock(sb, "Source .nss", nssPath, includeHash: true);
            AppendNssSnippet(sb, "Source NSS snippet", nssPath, 40);
            try
            {
                sb.Append("-- #include directives found in source --\n");
                foreach (string inc in ExtractIncludes(nssPath))
                {
                    sb.Append("  ").Append(inc).Append('\n');
                }

                sb.Append('\n');
            }
            catch
            {
                sb.Append("(could not enumerate includes)\n\n");
            }

            sb.Append("-- Managed compiler exception --\n");
            sb.Append(cause.ToString()).Append('\n');
            return sb.ToString();
        }

        private static string BuildRecompileDecompiledFailureMessage(
            string nssPath,
            string gameFlag,
            string compileInputPath,
            string expectedNcsOut,
            Exception cause)
        {
            var sb = new StringBuilder();
            sb.Append("═══════════════════════════════════════════════════════════════\n");
            sb.Append("RECOMPILE OF DECOMPILED .nss FAILED (nwnnsscomp)\n");
            sb.Append("═══════════════════════════════════════════════════════════════\n\n");
            sb.Append("After decompiling the original .ncs, the harness recompiles the decompiled NSS.\n");
            sb.Append("That recompile is required so bytecode can be compared to the original .ncs.\n\n");
            AppendFileDiagnosticBlock(sb, "Original source .nss (for reference)", nssPath, includeHash: false);
            AppendFileDiagnosticBlock(sb, "Decompiled NSS used as compiler input", compileInputPath, includeHash: true);
            AppendFileDiagnosticBlock(sb, "Expected .rt.ncs output", expectedNcsOut, includeHash: false);
            AppendNssSnippet(sb, "Decompiled NSS input snippet", compileInputPath, 50);
            sb.Append("-- nwnnsscomp exception --\n");
            sb.Append(cause.ToString()).Append('\n');
            return sb.ToString();
        }

        private static void MergeOperationTime(string key, long nanos)
        {
            lock (OperationTimes)
            {
                if (OperationTimes.TryGetValue(key, out long v))
                {
                    OperationTimes[key] = v + nanos;
                }
                else
                {
                    OperationTimes[key] = nanos;
                }
            }
        }

        private static void RoundTripSingle(string nssPath, string gameFlag, string scratchRoot)
        {
            long startTime = Stopwatch.GetTimestamp();

            string rel = Path.GetRelativePath(VanillaRepoDir, nssPath);
            string displayRelPath = rel.Replace('\\', '/');

            string parentRel = Path.GetDirectoryName(rel);
            if (string.IsNullOrEmpty(parentRel))
            {
                parentRel = string.Empty;
            }

            string outDir = Path.Combine(scratchRoot, parentRel);
            Directory.CreateDirectory(outDir);

            bool isK2 = string.Equals(gameFlag, "k2", StringComparison.Ordinal);
            string compiledManagedPath = Path.Combine(outDir, StripExt(Path.GetFileName(rel)) + ".kcompiler.ncs");

            string compiledFirst = Path.Combine(outDir, StripExt(Path.GetFileName(rel)) + ".ncs");
            Console.Write("  Compiling " + displayRelPath + " to .ncs with nwnnsscomp.exe");
            long compileOriginalStart = Stopwatch.GetTimestamp();
            try
            {
                RunCompiler(nssPath, compiledFirst, gameFlag, scratchRoot);
                long compileTime = Stopwatch.GetElapsedTime(compileOriginalStart).Ticks * 100;
                MergeOperationTime("compile-original", compileTime);
                MergeOperationTime("compile", compileTime);
                Console.WriteLine(" ✓ (" + string.Format("{0:F3}", compileTime / 1_000_000.0) + " ms)");
            }
            catch (Exception e)
            {
                long compileTime = Stopwatch.GetElapsedTime(compileOriginalStart).Ticks * 100;
                MergeOperationTime("compile-original", compileTime);
                MergeOperationTime("compile", compileTime);
                Console.WriteLine(" ✗ FAILED (original source file has compilation errors)");
                throw new InvalidOperationException(
                    BuildNwnnsscompOriginalCompileFailureMessage(nssPath, gameFlag, compiledFirst, displayRelPath, e),
                    e);
            }

            // Steps 6–7: managed KCompiler compile vs nwnnsscomp bytecode/pcode (fail fast on mismatch).
            Console.Write("  Compiling " + displayRelPath + " to .ncs with KCompiler (managed)");
            long compileManagedStart = Stopwatch.GetTimestamp();
            try
            {
                string nssSource = File.ReadAllText(nssPath, Encoding.UTF8);
                NCS managedNcs = NCSAuto.CompileNss(nssSource, GameFlagToGame(gameFlag), null, null, null);
                byte[] managedBytes = NCSAuto.BytesNcs(managedNcs);
                File.WriteAllBytes(compiledManagedPath, managedBytes);
                long compileTime = Stopwatch.GetElapsedTime(compileManagedStart).Ticks * 100;
                MergeOperationTime("compile-kcompiler", compileTime);
                MergeOperationTime("compile", compileTime);
                Console.WriteLine(" ✓ (" + string.Format("{0:F3}", compileTime / 1_000_000.0) + " ms)");
            }
            catch (Exception ex)
            {
                long compileTime = Stopwatch.GetElapsedTime(compileManagedStart).Ticks * 100;
                MergeOperationTime("compile-kcompiler", compileTime);
                MergeOperationTime("compile", compileTime);
                Console.WriteLine(" ✗ FAILED");
                throw new InvalidOperationException(
                    "KCompiler managed compile failed for " + DisplayPath(nssPath) + ": " + ex.Message,
                    ex);
            }

            AssertBytecodeEqual(
                compiledFirst,
                compiledManagedPath,
                gameFlag,
                displayRelPath + " [nwnnsscomp vs KCompiler]",
                contextDecompiledNssPath: null);

            string decompiled = Path.Combine(outDir, StripExt(Path.GetFileName(rel)) + ".dec.nss");
            Console.Write("  Decompiling " + Path.GetFileName(compiledFirst) + " back to .nss");
            long decompileStart = Stopwatch.GetTimestamp();
            try
            {
                RunDecompile(compiledFirst, decompiled, gameFlag);
                long decompileTime = Stopwatch.GetElapsedTime(decompileStart).Ticks * 100;
                MergeOperationTime("decompile", decompileTime);
                Console.WriteLine(" ✓ (" + string.Format("{0:F3}", decompileTime / 1_000_000.0) + " ms)");
            }
            catch (Exception)
            {
                long decompileTime = Stopwatch.GetElapsedTime(decompileStart).Ticks * 100;
                MergeOperationTime("decompile", decompileTime);
                throw;
            }

            Console.Write("  Comparing original vs decompiled (text)");
            long compareTextStart = Stopwatch.GetTimestamp();
            string originalExpanded = ExpandIncludes(nssPath, gameFlag);
            string roundtripRaw = File.ReadAllText(decompiled, Encoding.UTF8);

            string originalExpandedFiltered = FilterFunctionsNotInDecompiled(originalExpanded, roundtripRaw);

            string original = NormalizeNewlines(originalExpandedFiltered, isK2);
            string roundtrip = NormalizeNewlines(roundtripRaw, isK2);
            long compareTextElapsed = Stopwatch.GetElapsedTime(compareTextStart).Ticks * 100;
            MergeOperationTime("compare-text", compareTextElapsed);
            MergeOperationTime("compare", compareTextElapsed);

            if (!string.Equals(original, roundtrip, StringComparison.Ordinal))
            {
                Console.WriteLine(" ✗ MISMATCH");
                string diff = FormatUnifiedDiff(original, roundtrip);
                var sb = new StringBuilder();
                sb.Append("Round-trip text mismatch after normalization for ").Append(DisplayPath(nssPath)).Append("\n");
                if (diff != null)
                {
                    sb.Append(diff);
                }

                throw new InvalidOperationException(sb.ToString());
            }

            Console.WriteLine(" ✓ MATCH");

            // Steps 8–9: decompile KCompiler .ncs; normalized NSS must match external .ncs decompilation (step 3–4 path).
            string decompiledManaged = Path.Combine(outDir, StripExt(Path.GetFileName(rel)) + ".managed.dec.nss");
            Console.Write("  Decompiling KCompiler .ncs back to .nss");
            long decompileManagedStart = Stopwatch.GetTimestamp();
            try
            {
                RunDecompile(compiledManagedPath, decompiledManaged, gameFlag);
                long decompileManagedTime = Stopwatch.GetElapsedTime(decompileManagedStart).Ticks * 100;
                MergeOperationTime("decompile-kcompiler-ncs", decompileManagedTime);
                MergeOperationTime("decompile", decompileManagedTime);
                Console.WriteLine(" ✓ (" + string.Format("{0:F3}", decompileManagedTime / 1_000_000.0) + " ms)");
            }
            catch (Exception)
            {
                long decompileManagedTime = Stopwatch.GetElapsedTime(decompileManagedStart).Ticks * 100;
                MergeOperationTime("decompile-kcompiler-ncs", decompileManagedTime);
                MergeOperationTime("decompile", decompileManagedTime);
                throw;
            }

            Console.Write("  Comparing decompiled NSS (nwnnsscomp NCS vs KCompiler NCS, normalized)");
            string externalDecNorm = NormalizeNewlines(File.ReadAllText(decompiled, Encoding.UTF8), isK2);
            string managedDecNorm = NormalizeNewlines(File.ReadAllText(decompiledManaged, Encoding.UTF8), isK2);
            if (!string.Equals(externalDecNorm, managedDecNorm, StringComparison.Ordinal))
            {
                string diff = FormatUnifiedDiff(externalDecNorm, managedDecNorm);
                var sb = new StringBuilder();
                sb.Append("Decompiled NSS mismatch: nwnnsscomp-produced NCS vs KCompiler-produced NCS for ")
                    .Append(DisplayPath(nssPath))
                    .Append("\nFiles: ")
                    .Append(DisplayPath(decompiled))
                    .Append(" vs ")
                    .Append(DisplayPath(decompiledManaged))
                    .Append("\n");
                if (diff != null)
                {
                    sb.Append(diff);
                }

                Console.WriteLine(" ✗ MISMATCH");
                throw new InvalidOperationException(sb.ToString());
            }

            Console.WriteLine(" ✓ MATCH");

            string recompiled = Path.Combine(outDir, StripExt(Path.GetFileName(rel)) + ".rt.ncs");
            string compileInput = decompiled;
            string tempCompileInput = null;
            string decompiledName = Path.GetFileName(decompiled);
            if (decompiledName.IndexOf('.') != decompiledName.LastIndexOf('.'))
            {
                compileInput = Path.Combine(outDir, StripExt(Path.GetFileName(rel)) + "_dec.nss");
                File.Copy(decompiled, compileInput, true);
                tempCompileInput = compileInput;
            }

            Console.Write("  Recompiling decompiled .nss to .ncs");
            long compileRoundtripStart = Stopwatch.GetTimestamp();
            try
            {
                RunCompiler(compileInput, recompiled, gameFlag, scratchRoot);
                long compileTime = Stopwatch.GetElapsedTime(compileRoundtripStart).Ticks * 100;
                MergeOperationTime("compile-roundtrip", compileTime);
                MergeOperationTime("compile", compileTime);
                Console.WriteLine(" ✓ (" + string.Format("{0:F3}", compileTime / 1_000_000.0) + " ms)");
            }
            catch (Exception)
            {
                long compileTime = Stopwatch.GetElapsedTime(compileRoundtripStart).Ticks * 100;
                MergeOperationTime("compile-roundtrip", compileTime);
                MergeOperationTime("compile", compileTime);
                Console.WriteLine(" ✗ FAILED");
                throw;
            }
            finally
            {
                if (tempCompileInput != null)
                {
                    try
                    {
                        if (File.Exists(tempCompileInput))
                        {
                            File.Delete(tempCompileInput);
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            Console.Write("  Comparing bytecode (original vs recompiled)");
            long compareBytecodeStart = Stopwatch.GetTimestamp();
            try
            {
                AssertBytecodeEqual(compiledFirst, recompiled, gameFlag, displayRelPath, contextDecompiledNssPath: decompiled);
                long compareTime = Stopwatch.GetElapsedTime(compareBytecodeStart).Ticks * 100;
                MergeOperationTime("compare-bytecode", compareTime);
                MergeOperationTime("compare", compareTime);
                Console.WriteLine(" ✓ MATCH");
            }
            catch (Exception)
            {
                long compareTime = Stopwatch.GetElapsedTime(compareBytecodeStart).Ticks * 100;
                MergeOperationTime("compare-bytecode", compareTime);
                MergeOperationTime("compare", compareTime);
                throw;
            }

            long totalTime = Stopwatch.GetElapsedTime(startTime).Ticks * 100;
            MergeOperationTime("total", totalTime);
        }

        private static string StripExt(string name)
        {
            int dot = name.LastIndexOf('.');
            return dot == -1 ? name : name.Substring(0, dot);
        }

        private static void RunCompiler(string originalNssPath, string compiledOut, string gameFlag, string workDir)
        {
            string stagedSource = null;
            string nwscriptSource;
            if (string.Equals(gameFlag, "k1", StringComparison.Ordinal))
            {
                if (NeedsAscNwscript(originalNssPath))
                {
                    nwscriptSource = K1AscNwscript;
                    if (!File.Exists(nwscriptSource))
                    {
                        throw new InvalidOperationException("K1 ASC nwscript file not found: " + DisplayPath(nwscriptSource));
                    }
                }
                else
                {
                    nwscriptSource = K1Nwscript;
                    if (!File.Exists(nwscriptSource))
                    {
                        throw new InvalidOperationException("K1 nwscript file not found: " + DisplayPath(nwscriptSource));
                    }
                }
            }
            else if (string.Equals(gameFlag, "k2", StringComparison.Ordinal))
            {
                nwscriptSource = K2Nwscript;
                if (!File.Exists(nwscriptSource))
                {
                    throw new InvalidOperationException("TSL nwscript file not found: " + DisplayPath(nwscriptSource));
                }
            }
            else
            {
                throw new ArgumentException("Invalid game flag: " + gameFlag + " (expected 'k1' or 'k2')");
            }

            string compilerDir = Path.GetDirectoryName(NwnCompiler);
            if (string.IsNullOrEmpty(compilerDir))
            {
                throw new IOException("Compiler directory is null for: " + DisplayPath(NwnCompiler));
            }

            Directory.CreateDirectory(compilerDir);
            EnsureCompilerDirNwscript(compilerDir, nwscriptSource, gameFlag);
            Directory.CreateDirectory(Path.GetDirectoryName(compiledOut) ?? ".");

            stagedSource = Path.Combine(compilerDir, Path.GetFileName(originalNssPath));
            try
            {
                File.Copy(originalNssPath, stagedSource, true);
                StageIncludesIntoCompilerDir(stagedSource, originalNssPath, gameFlag, compilerDir);

                bool isK2 = string.Equals(gameFlag, "k2", StringComparison.Ordinal);
                var config = new NCSDecomp.Core.NwnnsscompConfig(NwnCompiler, stagedSource, compiledOut, isK2);
                string[] cmd = config.GetCompileArgs(Path.GetFullPath(NwnCompiler));

                string output = RunProcessWithTimeout(cmd, compilerDir, "nwnnsscomp compile for " + DisplayPath(originalNssPath));

                if (!File.Exists(compiledOut))
                {
                    Console.WriteLine(" ✗ FAILED");
                    throw new InvalidOperationException(
                        "nwnnsscomp failed (file missing) for " + DisplayPath(originalNssPath) + "\nCompiler output:\n" + output);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(stagedSource))
                {
                    try
                    {
                        if (File.Exists(stagedSource))
                        {
                            File.Delete(stagedSource);
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        private static void EnsureCompilerDirNwscript(string compilerDir, string nwscriptSource, string gameFlag)
        {
            if (string.IsNullOrEmpty(compilerDir))
            {
                throw new IOException("compilerDir is null");
            }

            string dest = Path.Combine(compilerDir, "nwscript.nss");
            if (gameFlag != null && string.Equals(gameFlag, _stagedNwscriptGameFlag, StringComparison.Ordinal) && File.Exists(dest))
            {
                return;
            }

            File.Copy(nwscriptSource, dest, true);
            _stagedNwscriptGameFlag = gameFlag;
        }

        private static void RunDecompile(string ncsPath, string nssOut, string gameFlag)
        {
            Console.Write(" (game=" + gameFlag + ", output=" + Path.GetFileName(nssOut) + ")");

            try
            {
                RoundTripUtil.DecompileNcsToNssFile(
                    ncsPath,
                    nssOut,
                    gameFlag,
                    new UTF8Encoding(false),
                    K1Nwscript,
                    K2Nwscript);

                if (!File.Exists(nssOut))
                {
                    Console.WriteLine(" ✗ FAILED - no output file created");
                    throw new InvalidOperationException("Decompile did not produce output: " + DisplayPath(nssOut));
                }
            }
            catch (DecompilerException ex)
            {
                Console.WriteLine(" ✗ FAILED - " + ex.Message);
                throw new InvalidOperationException("Decompile failed for " + DisplayPath(ncsPath) + ": " + ex.Message, ex);
            }
            catch (IOException ex)
            {
                Console.WriteLine(" ✗ FAILED - " + ex.Message);
                throw new InvalidOperationException("Decompile failed for " + DisplayPath(ncsPath) + ": " + ex.Message, ex);
            }
        }

        private static string PrepareScratch(string gameLabel, string nwscriptSource)
        {
            string scratch = Path.Combine(WorkRoot, gameLabel);
            Directory.CreateDirectory(scratch);
            string target = Path.Combine(scratch, "nwscript.nss");
            if (!File.Exists(target))
            {
                File.Copy(nwscriptSource, target, true);
            }

            return scratch;
        }

        private static string RunProcessWithTimeout(string[] cmd, string workingDir, string actionDescription)
        {
            if (cmd == null || cmd.Length == 0)
            {
                throw new ArgumentException("cmd");
            }

            var psi = new ProcessStartInfo
            {
                FileName = cmd[0],
                WorkingDirectory = workingDir ?? Environment.CurrentDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            for (int i = 1; i < cmd.Length; i++)
            {
                psi.ArgumentList.Add(cmd[i]);
            }

            using (var proc = new Process { StartInfo = psi })
            {
                var output = new StringBuilder();
                proc.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        lock (output)
                        {
                            output.AppendLine(e.Data);
                        }
                    }
                };
                proc.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        lock (output)
                        {
                            output.AppendLine(e.Data);
                        }
                    }
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                if (!proc.WaitForExit((int)ProcTimeout.TotalMilliseconds))
                {
                    try
                    {
                        proc.Kill(true);
                    }
                    catch
                    {
                        // ignore
                    }

                    throw new TimeoutException(actionDescription + " timed out after " + ProcTimeout.TotalSeconds + "s");
                }

                proc.WaitForExit();
                string text = output.ToString();
                if (proc.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        actionDescription + " failed with exit code " + proc.ExitCode + ".\nOutput:\n" + text);
                }

                return text;
            }
        }

        private static string ReadProcessStreams(Process proc)
        {
            var sb = new StringBuilder();
            Task<string> outTask = proc.StandardOutput.ReadToEndAsync();
            Task<string> errTask = proc.StandardError.ReadToEndAsync();
            Task.WaitAll(outTask, errTask);
            sb.Append(outTask.Result);
            sb.Append(errTask.Result);
            return sb.ToString();
        }

        private int RunRoundTripSuite(bool useResume)
        {
            ResetPerformanceTracking();
            _testStartTime = Stopwatch.GetTimestamp();
            long suiteStartNanos = Stopwatch.GetTimestamp();

            try
            {
                Preflight();
                List<RoundTripCase> tests = BuildRoundTripCases();

                if (tests.Count == 0)
                {
                    Console.Error.WriteLine("ERROR: No test files found!");
                    return 1;
                }

                string resumePoint = useResume ? LoadResumePoint() : null;
                int startIndex = 0;
                if (resumePoint != null)
                {
                    for (int i = 0; i < tests.Count; i++)
                    {
                        string displayPath = Path.GetRelativePath(VanillaRepoDir, tests[i].Item.Path).Replace('\\', '/');
                        if (string.Equals(displayPath, resumePoint, StringComparison.Ordinal) ||
                            string.Equals(tests[i].DisplayName, resumePoint, StringComparison.Ordinal))
                        {
                            startIndex = i;
                            Console.WriteLine("=== Resuming from last failure ===");
                            Console.WriteLine("Resume point: " + displayPath);
                            Console.WriteLine("Skipping " + i + " tests that already passed");
                            Console.WriteLine();
                            break;
                        }
                    }
                }

                Console.WriteLine("=== Running Round-Trip Tests ===");
                Console.WriteLine("Total tests: " + tests.Count);
                if (startIndex > 0)
                {
                    Console.WriteLine("Starting from test: " + (startIndex + 1));
                }

                Console.WriteLine("Fast-fail: enabled (will stop on first failure)");
                if (useResume)
                {
                    Console.WriteLine("Resume: enabled (use --no-resume to start from beginning)");
                }

                Console.WriteLine();

                _testsProcessed = startIndex;

                for (int i = startIndex; i < tests.Count; i++)
                {
                    RoundTripCase testCase = tests[i];
                    _testsProcessed++;
                    string displayPath = Path.GetRelativePath(VanillaRepoDir, testCase.Item.Path).Replace('\\', '/');

                    if (_maxSuiteNanos > 0)
                    {
                        long elapsed = (Stopwatch.GetTimestamp() - suiteStartNanos) * 1_000_000_000L / Stopwatch.Frequency;
                        if (elapsed > _maxSuiteNanos)
                        {
                            if (useResume)
                            {
                                SaveResumePoint(displayPath);
                                Console.WriteLine();
                                Console.WriteLine("=== TIME BUDGET REACHED ===");
                                Console.WriteLine("Saved resume point: " + displayPath);
                                Console.WriteLine("Re-run to continue.");
                                Console.WriteLine();
                            }

                            PrintPerformanceSummary();
                            return 0;
                        }
                    }

                    RoundTripResult result = RoundTripSingleWithOutputCapture(testCase.Item.Path, testCase.Item.GameFlag, testCase.Item.ScratchRoot);

                    if (result.Exception == null)
                    {
                        Console.WriteLine(string.Format("[{0}/{1}] {2} - PASS", _testsProcessed, _totalTests, displayPath));
                        if (useResume && _saveProgressEvery > 0 && (_testsProcessed % _saveProgressEvery) == 0)
                        {
                            if (i + 1 < tests.Count)
                            {
                                string nextDisplay = Path.GetRelativePath(VanillaRepoDir, tests[i + 1].Item.Path).Replace('\\', '/');
                                SaveResumePoint(nextDisplay);
                            }
                        }
                    }
                    else if (result.Exception is SourceCompilationException)
                    {
                        Console.WriteLine(string.Format("[{0}/{1}] {2} - SKIP (original source has compilation errors)", _testsProcessed, _totalTests, displayPath));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("[{0}/{1}] {2} - FAIL", _testsProcessed, _totalTests, displayPath));
                        Console.WriteLine();
                        if (!string.IsNullOrEmpty(result.CapturedOutput))
                        {
                            Console.Write(result.CapturedOutput);
                        }

                        PrintFailureBanner(testCase, result);
                        if (useResume)
                        {
                            SaveResumePoint(displayPath);
                            Console.WriteLine("Resume point saved. Next run will start from: " + displayPath);
                            Console.WriteLine("(Use --no-resume to start from beginning)");
                            Console.WriteLine();
                        }

                        PrintPerformanceSummary();
                        return 1;
                    }
                }

                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("ALL TESTS PASSED!");
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("Tests run: " + tests.Count);
                Console.WriteLine("Tests passed: " + tests.Count);
                Console.WriteLine("Tests failed: 0");
                Console.WriteLine();

                if (useResume)
                {
                    ClearResumePoint();
                }

                PrintPerformanceSummary();
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("FATAL ERROR: " + e.Message);
                Console.Error.WriteLine(e.ToString());
                PrintPerformanceSummary();
                return 1;
            }
        }

        /// <summary>Java <c>runRoundTripBytecodeSuite(boolean)</c> / no-arg overload (default <c>useResume</c> = true).</summary>
        public int RunRoundTripBytecodeSuite(bool useResume = true)
        {
            ResetPerformanceTracking();
            _testStartTime = Stopwatch.GetTimestamp();

            try
            {
                Preflight();
                List<RoundTripCase> tests = BuildRoundTripCases();

                if (tests.Count == 0)
                {
                    Console.Error.WriteLine("ERROR: No test files found!");
                    return 1;
                }

                string resumePoint = useResume ? LoadResumePoint() : null;
                int startIndex = 0;
                if (resumePoint != null)
                {
                    for (int i = 0; i < tests.Count; i++)
                    {
                        string displayPath = Path.GetRelativePath(VanillaRepoDir, tests[i].Item.Path).Replace('\\', '/');
                        if (string.Equals(displayPath, resumePoint, StringComparison.Ordinal) ||
                            string.Equals(tests[i].DisplayName, resumePoint, StringComparison.Ordinal))
                        {
                            startIndex = i;
                            Console.WriteLine("=== Resuming from last failure ===");
                            Console.WriteLine("Resume point: " + displayPath);
                            Console.WriteLine("Skipping " + i + " tests that already passed");
                            Console.WriteLine();
                            break;
                        }
                    }
                }

                Console.WriteLine("=== Running Bytecode Round-Trip Tests (NSS -> NCS -> NSS -> NCS) ===");
                Console.WriteLine("Total tests: " + tests.Count);
                if (startIndex > 0)
                {
                    Console.WriteLine("Starting from test: " + (startIndex + 1));
                }

                Console.WriteLine("Fast-fail: enabled (will stop on first failure)");
                if (useResume)
                {
                    Console.WriteLine("Resume: enabled (use --no-resume to start from beginning)");
                }

                Console.WriteLine();

                _testsProcessed = startIndex;

                for (int i = startIndex; i < tests.Count; i++)
                {
                    RoundTripCase testCase = tests[i];
                    _testsProcessed++;
                    string displayPath = Path.GetRelativePath(VanillaRepoDir, testCase.Item.Path).Replace('\\', '/');

                    RoundTripResult result = RoundTripSingleWithOutputCapture(testCase.Item.Path, testCase.Item.GameFlag, testCase.Item.ScratchRoot);

                    if (result.Exception == null)
                    {
                        Console.WriteLine(string.Format("[{0}/{1}] {2} - PASS", _testsProcessed, _totalTests, displayPath));
                    }
                    else if (result.Exception is SourceCompilationException)
                    {
                        Console.WriteLine(string.Format("[{0}/{1}] {2} - SKIP (original source has compilation errors)", _testsProcessed, _totalTests, displayPath));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("[{0}/{1}] {2} - FAIL", _testsProcessed, _totalTests, displayPath));
                        Console.WriteLine();
                        if (!string.IsNullOrEmpty(result.CapturedOutput))
                        {
                            Console.Write(result.CapturedOutput);
                        }

                        Console.WriteLine("═══════════════════════════════════════════════════════════");
                        Console.WriteLine("BYTECODE FAILURE: " + testCase.DisplayName);
                        Console.WriteLine("═══════════════════════════════════════════════════════════");
                        Console.WriteLine("Exception: " + result.Exception.GetType().Name);
                        if (!string.IsNullOrEmpty(result.Exception.Message))
                        {
                            Console.WriteLine("Message: " + result.Exception.Message);
                        }

                        if (result.Exception.InnerException != null && !ReferenceEquals(result.Exception.InnerException, result.Exception))
                        {
                            Console.WriteLine("Cause: " + result.Exception.InnerException.Message);
                        }

                        Console.WriteLine("═══════════════════════════════════════════════════════════");
                        Console.WriteLine();

                        PrintPerformanceSummary();
                        return 1;
                    }
                }

                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("ALL BYTECODE TESTS PASSED!");
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("Tests run: " + tests.Count);
                Console.WriteLine("Tests passed: " + tests.Count);
                Console.WriteLine("Tests failed: 0");
                Console.WriteLine();

                PrintPerformanceSummary();
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("FATAL ERROR: " + e.Message);
                Console.Error.WriteLine(e.ToString());
                PrintPerformanceSummary();
                return 1;
            }
        }

        private static void PrintFailureBanner(RoundTripCase testCase, RoundTripResult result)
        {
            string message = result.Exception.Message;
            if (!string.IsNullOrEmpty(message))
            {
                if (message.IndexOf("═══════════════════════════════════════════════════════════════", StringComparison.Ordinal) >= 0)
                {
                    Console.WriteLine(message);
                }
                else
                {
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    Console.WriteLine("FAILURE: " + testCase.DisplayName);
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    Console.WriteLine("Exception: " + result.Exception.GetType().Name);
                    string diff = ExtractAndFormatDiff(message);
                    if (diff != null)
                    {
                        Console.WriteLine("\nDiff:");
                        Console.WriteLine(diff);
                    }
                    else
                    {
                        Console.WriteLine("Message:\n" + message);
                    }

                    if (result.Exception.InnerException != null && !ReferenceEquals(result.Exception.InnerException, result.Exception))
                    {
                        Console.WriteLine("\nCause: " + result.Exception.InnerException.Message);
                    }

                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                }
            }
            else
            {
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("FAILURE: " + testCase.DisplayName);
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("Exception: " + result.Exception.GetType().Name);
                Console.Error.WriteLine(result.Exception.ToString());
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
            }
        }

        private int TestSingleFile(string filename, string gameFlag)
        {
            try
            {
                string foundFile = null;

                if (Path.IsPathRooted(filename) || filename.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0)
                {
                    foundFile = Path.GetFullPath(filename);
                    if (!File.Exists(foundFile))
                    {
                        Console.Error.WriteLine("ERROR: File not found: " + filename);
                        return 1;
                    }
                }
                else
                {
                    string searchDir = string.Equals(gameFlag, "k1", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(gameFlag, "K1", StringComparison.OrdinalIgnoreCase)
                        ? Path.Combine(VanillaRepoDir, "K1")
                        : Path.Combine(VanillaRepoDir, "TSL");

                    foundFile = Directory.EnumerateFiles(searchDir, "*", SearchOption.AllDirectories)
                        .FirstOrDefault(p => string.Equals(Path.GetFileName(p), filename, StringComparison.OrdinalIgnoreCase));
                    if (string.IsNullOrEmpty(foundFile))
                    {
                        Console.Error.WriteLine("ERROR: File not found: " + filename);
                        Console.Error.WriteLine("Searched in: " + searchDir);
                        return 1;
                    }
                }

                Console.WriteLine("=== Testing Single File ===");
                Console.WriteLine("File: " + foundFile);
                Console.WriteLine("Game: " + gameFlag);
                Console.WriteLine();

                string scratchRoot = Path.Combine(TestWorkDir, "roundtrip-work", gameFlag.ToLowerInvariant());
                RoundTripSingle(foundFile, gameFlag.ToLowerInvariant(), scratchRoot);

                Console.WriteLine();
                Console.WriteLine("✓ PASSED - Round-trip successful (bytecode matches)");
                return 0;
            }
            catch (SourceCompilationException e)
            {
                Console.Error.WriteLine("✗ SKIP: " + e.Message);
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("✗ FAILED");
                Console.Error.WriteLine();
                Console.Error.WriteLine(e.ToString());
                return 1;
            }
        }

        private static void SaveResumePoint(string testIdentifier)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ResumeFile) ?? TestWorkDir);
                File.WriteAllText(ResumeFile, testIdentifier, Encoding.UTF8);
            }
            catch (IOException e)
            {
                Console.Error.WriteLine("Warning: Could not save resume point: " + e.Message);
            }
        }

        private static string LoadResumePoint()
        {
            try
            {
                if (File.Exists(ResumeFile))
                {
                    string content = File.ReadAllText(ResumeFile, Encoding.UTF8).Trim();
                    if (content.Length > 0)
                    {
                        return content;
                    }
                }
            }
            catch (IOException)
            {
                // ignore
            }

            return null;
        }

        private static void ClearResumePoint()
        {
            try
            {
                if (File.Exists(ResumeFile))
                {
                    File.Delete(ResumeFile);
                }
            }
            catch (IOException)
            {
                // ignore
            }
        }

        private void PrintPerformanceSummary()
        {
            long totalTime = (Stopwatch.GetTimestamp() - _testStartTime) * 1_000_000_000L / Stopwatch.Frequency;

            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("PERFORMANCE SUMMARY");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine(string.Format("Total test time: {0:F2} seconds", totalTime / 1_000_000_000.0));
            Console.WriteLine(string.Format("Tests processed: {0} / {1}", _testsProcessed, _totalTests));

            if (_testsProcessed > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Operation breakdown (cumulative):");
                lock (OperationTimes)
                {
                    foreach (KeyValuePair<string, long> entry in OperationTimes)
                    {
                        double seconds = entry.Value / 1_000_000_000.0;
                        double percentage = totalTime > 0 ? entry.Value * 100.0 / totalTime : 0;
                        Console.WriteLine(string.Format("  {0,-12}: {1,8:F2} s ({2,5:F1}%)", entry.Key, seconds, percentage));
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Average per test:");
                long totalOp;
                long compileOp;
                long decompileOp;
                long compareOp;
                lock (OperationTimes)
                {
                    totalOp = OperationTimes.ContainsKey("total") ? OperationTimes["total"] : 0;
                    compileOp = OperationTimes.ContainsKey("compile") ? OperationTimes["compile"] : 0;
                    decompileOp = OperationTimes.ContainsKey("decompile") ? OperationTimes["decompile"] : 0;
                    compareOp = OperationTimes.ContainsKey("compare") ? OperationTimes["compare"] : 0;
                }

                Console.WriteLine(string.Format("  Total:      {0:F3} s", (totalOp / 1_000_000_000.0) / _testsProcessed));
                Console.WriteLine(string.Format("  Compile:    {0:F3} s", (compileOp / 1_000_000_000.0) / _testsProcessed));
                Console.WriteLine(string.Format("  Decompile:  {0:F3} s", (decompileOp / 1_000_000_000.0) / _testsProcessed));
                Console.WriteLine(string.Format("  Compare:    {0:F3} s", (compareOp / 1_000_000_000.0) / _testsProcessed));
            }

            Console.WriteLine();
            Console.WriteLine("Profile log: " + DisplayPath(ProfileOutput));
            Console.WriteLine("═══════════════════════════════════════════════════════════");
        }

        private static string ExtractAndFormatDiff(string message)
        {
            int expectedStart = message.IndexOf("expected: <", StringComparison.Ordinal);
            int butWasStart = message.IndexOf(" but was: <", StringComparison.Ordinal);
            if (expectedStart == -1 || butWasStart == -1)
            {
                return null;
            }

            int expectedValueStart = expectedStart + "expected: <".Length;
            int expectedValueEnd = message.IndexOf(">", expectedValueStart, StringComparison.Ordinal);
            int actualValueStart = butWasStart + " but was: <".Length;
            int actualValueEnd = message.LastIndexOf('>');
            if (expectedValueEnd == -1 || actualValueEnd == -1 || actualValueEnd <= actualValueStart)
            {
                return null;
            }

            string expected = message.Substring(expectedValueStart, expectedValueEnd - expectedValueStart);
            string actual = message.Substring(actualValueStart, actualValueEnd - actualValueStart);
            return FormatUnifiedDiff(expected, actual);
        }

        private static void DeleteDirectory(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch (IOException)
            {
                // ignore
            }
        }

        // --- merged from NCSDecompCliRoundTripTest.PortIncludes.cs ---
        private static Dictionary<string, string> LoadConstantsWithPrefix(string path, string prefix)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            LoadConstantsFromFile(path, prefix, map);
            return map;
        }

        private static void LoadConstantsFromFile(string path, string prefix, Dictionary<string, string> map)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                var p = new Regex("^\\s*int\\s+(" + Regex.Escape(prefix) + "[A-Za-z0-9_]+)\\s*=\\s*([-]?[0-9]+)\\s*;.*$");
                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    Match m = p.Match(line);
                    if (m.Success)
                    {
                        map[m.Groups[1].Value] = m.Groups[2].Value;
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private static bool NeedsAscNwscript(string nssPath)
        {
            string content = File.ReadAllText(nssPath, Encoding.UTF8);
            var pattern = new Regex(
                "ActionStartConversation\\s*\\(([^,)]*,\\s*){10}[^)]*\\)",
                RegexOptions.Multiline);
            return pattern.IsMatch(content);
        }

        private static List<string> ExtractIncludes(string nssPath)
        {
            string content = File.ReadAllText(nssPath, Encoding.UTF8);
            var includes = new List<string>();
            var includePattern = new Regex("#include\\s+[\"<]([^\">]+)[\">]", RegexOptions.Multiline);
            foreach (Match m in includePattern.Matches(content))
            {
                includes.Add(m.Groups[1].Value);
            }

            return includes;
        }

        private static string FindIncludeFile(string includeName, string sourceFile, string gameFlag)
        {
            string normalizedName = includeName;
            if (!normalizedName.EndsWith(".nss", StringComparison.OrdinalIgnoreCase) &&
                !normalizedName.EndsWith(".h", StringComparison.OrdinalIgnoreCase))
            {
                normalizedName = includeName + ".nss";
            }

            string sourceDir = Path.GetDirectoryName(sourceFile);
            if (!string.IsNullOrEmpty(sourceDir))
            {
                string localInc = Path.Combine(sourceDir, normalizedName);
                if (File.Exists(localInc))
                {
                    return localInc;
                }

                localInc = Path.Combine(sourceDir, includeName);
                if (File.Exists(localInc))
                {
                    return localInc;
                }
            }

            if (string.Equals(gameFlag, "k2", StringComparison.Ordinal))
            {
                string tslScriptsDir = Path.Combine(VanillaRepoDir, "TSL", "Vanilla", "Data", "Scripts");
                string tslInc = Path.Combine(tslScriptsDir, normalizedName);
                if (File.Exists(tslInc))
                {
                    return tslInc;
                }

                tslInc = Path.Combine(tslScriptsDir, includeName);
                if (File.Exists(tslInc))
                {
                    return tslInc;
                }

                string tslRcmScriptsDir = Path.Combine(VanillaRepoDir, "TSL", "TSLRCM", "Data", "Scripts");
                string tslRcmInc = Path.Combine(tslRcmScriptsDir, normalizedName);
                if (File.Exists(tslRcmInc))
                {
                    return tslRcmInc;
                }

                tslRcmInc = Path.Combine(tslRcmScriptsDir, includeName);
                if (File.Exists(tslRcmInc))
                {
                    return tslRcmInc;
                }
            }

            string k1IncludesDir = Path.Combine(VanillaRepoDir, "K1", "Data", "scripts.bif");
            string k1Inc = Path.Combine(k1IncludesDir, normalizedName);
            if (File.Exists(k1Inc))
            {
                return k1Inc;
            }

            k1Inc = Path.Combine(k1IncludesDir, includeName);
            if (File.Exists(k1Inc))
            {
                return k1Inc;
            }

            return null;
        }

        private static void StageIncludesIntoCompilerDir(string stagedSourcePath, string originalSourcePath, string gameFlag, string compilerDir)
        {
            try
            {
                var work = new Queue<string>();
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                work.Enqueue(originalSourcePath);

                while (work.Count > 0)
                {
                    string current = work.Dequeue();
                    if (string.IsNullOrEmpty(current) || !visited.Add(Path.GetFullPath(current)))
                    {
                        continue;
                    }

                    if (!File.Exists(current))
                    {
                        continue;
                    }

                    foreach (string includeName in ExtractIncludes(current))
                    {
                        if (string.IsNullOrWhiteSpace(includeName))
                        {
                            continue;
                        }

                        string normalized = includeName.Trim();
                        string key = (gameFlag ?? string.Empty) + ":" + normalized.ToLowerInvariant();
                        if (StagedIncludes.ContainsKey(key))
                        {
                            continue;
                        }

                        string includeFile = FindIncludeFile(normalized, current, gameFlag);
                        if (string.IsNullOrEmpty(includeFile) || !File.Exists(includeFile))
                        {
                            StagedIncludes.TryAdd(key, 0);
                            continue;
                        }

                        CopyIncludeFile(normalized, includeFile, compilerDir);
                        StagedIncludes.TryAdd(key, 0);
                        work.Enqueue(includeFile);
                    }
                }
            }
            catch
            {
                // best-effort
            }
        }

        private static void CopyIncludeFile(string includeName, string includeFile, string tempDir)
        {
            string includeTarget = Path.Combine(tempDir, includeName);
            string parent = Path.GetDirectoryName(includeTarget);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }
            else
            {
                Directory.CreateDirectory(tempDir);
            }

            File.Copy(includeFile, includeTarget, true);

            if (includeName.IndexOf('.') < 0)
            {
                string fileName = Path.GetFileName(includeFile);
                int dotIdx = fileName.LastIndexOf('.');
                if (dotIdx >= 0)
                {
                    string ext = fileName.Substring(dotIdx);
                    string altTarget = Path.Combine(tempDir, includeName + ext);
                    string altParent = Path.GetDirectoryName(altTarget);
                    if (!string.IsNullOrEmpty(altParent))
                    {
                        Directory.CreateDirectory(altParent);
                    }

                    File.Copy(includeFile, altTarget, true);
                }
            }
        }

        private static string ExpandIncludes(string sourceFile, string gameFlag)
        {
            return ExpandIncludesInternal(sourceFile, gameFlag, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        private static string ExpandIncludesInternal(string sourceFile, string gameFlag, HashSet<string> visited)
        {
            string normalizedSource = Path.GetFullPath(sourceFile);
            if (!visited.Add(normalizedSource))
            {
                return string.Empty;
            }

            string content = File.ReadAllText(normalizedSource, Encoding.UTF8);
            var expanded = new StringBuilder();
            var includePattern = new Regex("#include\\s+[\"<]([^\">]+)[\">]");
            using (var reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Match matcher = includePattern.Match(line);
                    if (matcher.Success)
                    {
                        string includeName = matcher.Groups[1].Value;
                        string includeFile = FindIncludeFile(includeName, normalizedSource, gameFlag);
                        if (!string.IsNullOrEmpty(includeFile) && File.Exists(includeFile))
                        {
                            expanded.Append(ExpandIncludesInternal(includeFile, gameFlag, visited));
                            expanded.Append("\n");
                        }

                        continue;
                    }

                    expanded.Append(line).Append("\n");
                }
            }

            return expanded.ToString();
        }

        /// <summary>Java <c>copyIncludesRecursive</c> — parity copy; not used by optimized <see cref="RunCompiler"/> path.</summary>
        private static void CopyIncludesRecursive(string sourceFile, string gameFlag, string tempDir)
        {
            var worklist = new Queue<string>();
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var copied = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            worklist.Enqueue(sourceFile);
            while (worklist.Count > 0)
            {
                string current = worklist.Dequeue();
                if (string.IsNullOrEmpty(current) || !File.Exists(current))
                {
                    continue;
                }

                string normalized = Path.GetFullPath(current);
                if (!processed.Add(normalized))
                {
                    continue;
                }

                foreach (string includeName in ExtractIncludes(current))
                {
                    string includeFile = FindIncludeFile(includeName, current, gameFlag);
                    if (string.IsNullOrEmpty(includeFile) || !File.Exists(includeFile))
                    {
                        continue;
                    }

                    string key = includeName.ToLowerInvariant();
                    if (!copied.ContainsKey(key))
                    {
                        CopyIncludeFile(includeName, includeFile, tempDir);
                        copied[key] = includeFile;
                    }

                    worklist.Enqueue(includeFile);
                }
            }
        }

        /// <summary>Java <c>setupTempCompileDir</c> — parity helper; not used by optimized compiler staging.</summary>
        private static string SetupTempCompileDir(string originalNssPath, string gameFlag)
        {
            Directory.CreateDirectory(CompileTempRoot);
            string tempDir = Path.Combine(CompileTempRoot, "compile_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string tempSourceFile = Path.Combine(tempDir, Path.GetFileName(originalNssPath));
            File.Copy(originalNssPath, tempSourceFile, true);
            CopyIncludesRecursive(originalNssPath, gameFlag, tempDir);
            return tempDir;
        }

        // --- merged from NCSDecompCliRoundTripTest.PortFilter.cs ---
        private static string FilterFunctionsNotInDecompiled(string expandedOriginal, string decompiledOutput)
        {
            int decompiledFunctionCount = CountNonMainFunctions(decompiledOutput);
            if (decompiledFunctionCount == 0)
            {
                return expandedOriginal;
            }

            Dictionary<string, int> decompiledSignatureCounts = ExtractFunctionSignatures(decompiledOutput);
            List<string> originalCallOrder = ExtractFunctionCallOrder(expandedOriginal);
            List<string> decompiledCallOrder = ExtractFunctionCallOrder(decompiledOutput);
            return FilterFunctionsByCallOrderAndSignatures(
                expandedOriginal,
                decompiledSignatureCounts,
                originalCallOrder,
                decompiledCallOrder,
                decompiledFunctionCount);
        }

        private static int CountNonMainFunctions(string code)
        {
            int count = 0;
            var funcPattern = new Regex("^(\\s*)(\\w+)\\s+(\\w+)\\s*\\([^)]*\\)\\s*\\{", RegexOptions.Multiline);
            foreach (Match m in funcPattern.Matches(code))
            {
                string funcName = m.Groups[3].Value;
                if (!string.Equals(funcName, "main", StringComparison.Ordinal) &&
                    !string.Equals(funcName, "StartingConditional", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static List<string> ExtractFunctionCallOrder(string code)
        {
            var calledFunctions = new List<string>();
            var mainPattern = new Regex("void\\s+main\\s*\\([^)]*\\)\\s*\\{", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Match mainMatcher = mainPattern.Match(code);
            if (!mainMatcher.Success)
            {
                return calledFunctions;
            }

            int mainStart = mainMatcher.Index + mainMatcher.Length;
            int depth = 1;
            int pos = mainStart;
            while (pos < code.Length && depth > 0)
            {
                if (code[pos] == '{')
                {
                    depth++;
                }
                else if (code[pos] == '}')
                {
                    depth--;
                }

                pos++;
            }

            string mainBody = code.Substring(mainStart, pos - mainStart);
            var callPattern = new Regex("\\b([a-zA-Z_][a-zA-Z0-9_]*)\\s*\\(");
            foreach (Match callMatcher in callPattern.Matches(mainBody))
            {
                string funcName = callMatcher.Groups[1].Value;
                if (string.Equals(funcName, "main", StringComparison.Ordinal) ||
                    string.Equals(funcName, "if", StringComparison.Ordinal) ||
                    string.Equals(funcName, "while", StringComparison.Ordinal) ||
                    string.Equals(funcName, "for", StringComparison.Ordinal) ||
                    string.Equals(funcName, "return", StringComparison.Ordinal) ||
                    string.Equals(funcName, "GetModule", StringComparison.Ordinal) ||
                    string.Equals(funcName, "GetFirstPC", StringComparison.Ordinal) ||
                    string.Equals(funcName, "GetPartyMemberByIndex", StringComparison.Ordinal) ||
                    string.Equals(funcName, "SKILL_COMPUTER_USE", StringComparison.Ordinal) ||
                    string.Equals(funcName, "SW_PLOT_COMPUTER_DEACTIVATE_TURRETS", StringComparison.Ordinal) ||
                    string.Equals(funcName, "TRUE", StringComparison.Ordinal) ||
                    Regex.IsMatch(funcName, "^(int|float|string|object|vector|location|effect|itemproperty|talent|action|event)\\d+$") ||
                    funcName.StartsWith("intGLOB_", StringComparison.Ordinal) ||
                    funcName.StartsWith("__unknown_param", StringComparison.Ordinal))
                {
                    continue;
                }

                calledFunctions.Add(funcName.ToLowerInvariant());
            }

            return calledFunctions;
        }

        private static Dictionary<string, int> ExtractFunctionSignatures(string code)
        {
            var signatureCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var funcPattern = new Regex("^(\\s*)(\\w+)\\s+(\\w+)\\s*\\([^)]*\\)\\s*\\{", RegexOptions.Multiline);
            foreach (Match m in funcPattern.Matches(code))
            {
                string returnType = m.Groups[2].Value;
                string fullMatch = m.Groups[0].Value;
                int paramCount = 0;
                int paramStart = fullMatch.IndexOf('(');
                int paramEnd = fullMatch.IndexOf(')', paramStart);
                if (paramStart >= 0 && paramEnd > paramStart)
                {
                    string paramList = fullMatch.Substring(paramStart + 1, paramEnd - paramStart - 1).Trim();
                    if (paramList.Length > 0)
                    {
                        paramCount = paramList.Split(',').Length;
                    }
                }

                string signature = returnType.ToLowerInvariant() + "/" + paramCount;
                if (signatureCounts.ContainsKey(signature))
                {
                    signatureCounts[signature]++;
                }
                else
                {
                    signatureCounts[signature] = 1;
                }
            }

            return signatureCounts;
        }

        private static string FilterFunctionsByCallOrderAndSignatures(
            string code,
            Dictionary<string, int> decompiledSignatureCounts,
            List<string> originalCallOrder,
            List<string> decompiledCallOrder,
            int decompiledFunctionCount)
        {
            List<FunctionInfo> allFunctions = ExtractAllFunctions(code);
            var functionMap = new Dictionary<string, FunctionInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (FunctionInfo func in allFunctions)
            {
                functionMap[func.Name.ToLowerInvariant()] = func;
            }

            var matchedFunctions = new List<FunctionInfo>();
            int minCalls = Math.Min(originalCallOrder.Count, decompiledCallOrder.Count);
            for (int i = 0; i < minCalls; i++)
            {
                string originalFuncName = originalCallOrder[i];
                if (functionMap.TryGetValue(originalFuncName, out FunctionInfo func) &&
                    !string.Equals(func.Name, "main", StringComparison.Ordinal) &&
                    !string.Equals(func.Name, "StartingConditional", StringComparison.Ordinal))
                {
                    matchedFunctions.Add(func);
                }
            }

            var result = new System.Text.StringBuilder();
            var keptCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var keptFunctions = new HashSet<FunctionInfo>();

            int firstFuncPos = code.Length;
            foreach (FunctionInfo func in allFunctions)
            {
                if (func.StartPos < firstFuncPos)
                {
                    firstFuncPos = func.StartPos;
                }
            }

            if (firstFuncPos > 0)
            {
                result.Append(code.Substring(0, firstFuncPos));
            }

            foreach (FunctionInfo func in matchedFunctions)
            {
                if (keptFunctions.Count < decompiledFunctionCount)
                {
                    result.Append(func.FullText);
                    keptFunctions.Add(func);
                    string sig = func.ReturnType.ToLowerInvariant() + "/" + func.ParamCount;
                    if (keptCounts.ContainsKey(sig))
                    {
                        keptCounts[sig]++;
                    }
                    else
                    {
                        keptCounts[sig] = 1;
                    }
                }
            }

            foreach (FunctionInfo func in allFunctions)
            {
                if (string.Equals(func.Name, "main", StringComparison.Ordinal) ||
                    string.Equals(func.Name, "StartingConditional", StringComparison.Ordinal))
                {
                    continue;
                }

                if (keptFunctions.Contains(func))
                {
                    continue;
                }

                if (keptFunctions.Count >= decompiledFunctionCount)
                {
                    break;
                }

                string sig = func.ReturnType.ToLowerInvariant() + "/" + func.ParamCount;
                int maxAllowed = decompiledSignatureCounts.ContainsKey(sig) ? decompiledSignatureCounts[sig] : 0;
                int alreadyKept = keptCounts.ContainsKey(sig) ? keptCounts[sig] : 0;
                if (alreadyKept < maxAllowed)
                {
                    result.Append(func.FullText);
                    keptFunctions.Add(func);
                    keptCounts[sig] = alreadyKept + 1;
                }
            }

            foreach (FunctionInfo func in allFunctions)
            {
                if (string.Equals(func.Name, "main", StringComparison.Ordinal) ||
                    string.Equals(func.Name, "StartingConditional", StringComparison.Ordinal))
                {
                    result.Append(func.FullText);
                    break;
                }
            }

            return result.ToString();
        }

        private sealed class FunctionInfo
        {
            public FunctionInfo(string name, string returnType, int paramCount, int startPos, string fullText)
            {
                Name = name;
                ReturnType = returnType;
                ParamCount = paramCount;
                StartPos = startPos;
                FullText = fullText;
            }

            public string Name { get; }
            public string ReturnType { get; }
            public int ParamCount { get; }
            public int StartPos { get; }
            public string FullText { get; }
        }

        private static List<FunctionInfo> ExtractAllFunctions(string code)
        {
            var functions = new List<FunctionInfo>();
            var funcPattern = new Regex("^(\\s*)(\\w+)\\s+(\\w+)\\s*\\([^)]*\\)\\s*\\{", RegexOptions.Multiline);
            foreach (Match m in funcPattern.Matches(code))
            {
                string returnType = m.Groups[2].Value;
                string funcName = m.Groups[3].Value;
                int funcStart = m.Index;
                string fullMatch = m.Groups[0].Value;
                int paramCount = 0;
                int paramStart = fullMatch.IndexOf('(');
                int paramEnd = fullMatch.IndexOf(')', paramStart);
                if (paramStart >= 0 && paramEnd > paramStart)
                {
                    string paramList = fullMatch.Substring(paramStart + 1, paramEnd - paramStart - 1).Trim();
                    if (paramList.Length > 0)
                    {
                        paramCount = paramList.Split(',').Length;
                    }
                }

                int depth = 0;
                int pos = funcStart;
                while (pos < code.Length)
                {
                    if (code[pos] == '{')
                    {
                        depth++;
                    }
                    else if (code[pos] == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            pos++;
                            break;
                        }
                    }

                    pos++;
                }

                string fullText = code.Substring(funcStart, pos - funcStart);
                functions.Add(new FunctionInfo(funcName, returnType, paramCount, funcStart, fullText));
            }

            return functions;
        }

        // --- merged from NCSDecompCliRoundTripTest.PortNormalize.cs ---
        private static string NormalizeNewlines(string s, bool isK2)
        {
            string normalized = s.Replace("\r\n", "\n").Replace("\r", "\n");
            normalized = StripComments(normalized);
            normalized = NormalizeIncludes(normalized);
            normalized = NormalizeDeclarationAssignment(normalized);
            normalized = NormalizeLeadingPlaceholders(normalized);
            normalized = NormalizePlaceholderGlobals(normalized);
            normalized = NormalizeVariableNames(normalized);
            normalized = NormalizeFunctionBraces(normalized);
            normalized = NormalizeTrailingZeroParams(normalized);
            normalized = NormalizeTrailingDefaults(normalized);
            normalized = NormalizeEffectDeathDefaults(normalized);
            normalized = NormalizeLogicalOperators(normalized);
            normalized = NormalizeIfSpacing(normalized);
            normalized = NormalizeDoubleParensInCalls(normalized);
            normalized = NormalizeAssignmentParens(normalized);
            normalized = NormalizeCallArgumentParens(normalized);
            normalized = NormalizeStructNames(normalized);
            normalized = NormalizeSubroutineNames(normalized);
            normalized = NormalizePrototypeDecls(normalized);
            normalized = NormalizeReturnStatements(normalized);
            normalized = NormalizeFunctionSignaturesByArity(normalized);
            normalized = NormalizeComparisonParens(normalized);
            normalized = NormalizeTrueFalse(normalized);
            normalized = NormalizeConstants(normalized, isK2 ? NpcConstantsK2 : NpcConstantsK1);
            normalized = NormalizeConstants(normalized, isK2 ? AbilityConstantsK2 : AbilityConstantsK1);
            normalized = NormalizeConstants(normalized, isK2 ? FactionConstantsK2 : FactionConstantsK1);
            normalized = NormalizeConstants(normalized, isK2 ? AnimationConstantsK2 : AnimationConstantsK1);
            normalized = NormalizeBitwiseOperators(normalized);
            normalized = NormalizeControlFlowConditions(normalized);
            normalized = NormalizeCommaSpacing(normalized);
            normalized = NormalizePlaceholderNames(normalized);
            normalized = NormalizeAssignCommandPlaceholders(normalized);
            normalized = NormalizeFunctionOrder(normalized);

            string[] lines = normalized.Split(new[] { '\n' }, StringSplitOptions.None);
            var result = new StringBuilder();
            foreach (string line in lines)
            {
                string trimmed = Regex.Replace(Regex.Replace(line, "^\\s+", ""), "\\s+$", "");
                if (trimmed.Length == 0)
                {
                    continue;
                }

                trimmed = trimmed.Replace("\t", "    ");
                result.Append(trimmed).Append("\n");
            }

            string finalResult = result.ToString();
            while (finalResult.Length > 0 && finalResult[finalResult.Length - 1] == '\n')
            {
                finalResult = finalResult.Substring(0, finalResult.Length - 1);
            }

            return finalResult;
        }

        private static string NormalizeTrailingDefaults(string code)
        {
            var trailingDefaults = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "ActionJumpToObject", "(1|TRUE)" },
                { "d2", "(0|1)" },
                { "d3", "(0|1)" },
                { "d4", "(0|1)" },
                { "d6", "(0|1)" },
                { "d8", "(0|1)" },
                { "d10", "(0|1)" },
                { "d12", "(0|1)" },
                { "d20", "(0|1)" },
                { "d100", "(0|1)" },
                { "ActionAttack", "(0|FALSE)" },
                { "ActionStartConversation", "(0|0xFFFFFFFF|-1)" },
                { "ActionMoveToObject", "(1\\.0|1)" }
            };

            string result = code;
            foreach (KeyValuePair<string, string> entry in trailingDefaults)
            {
                string func = entry.Key;
                string defaultValue = entry.Value;
                var pattern = new Regex(
                    "(" + Regex.Escape(func) + "\\s*\\([^)]*),\\s*" + defaultValue + "\\s*\\)");
                var matches = new List<Tuple<int, int, string>>();
                foreach (Match m in pattern.Matches(result))
                {
                    matches.Add(Tuple.Create(m.Index, m.Index + m.Length, m.Groups[1].Value));
                }

                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    Tuple<int, int, string> match = matches[i];
                    int start = match.Item1;
                    int end = match.Item2;
                    string group1 = match.Item3;
                    if (start >= 0 && end <= result.Length && start < end)
                    {
                        result = result.Substring(0, start) + group1 + ")" + result.Substring(end);
                    }
                }
            }

            return result;
        }

        private static string NormalizeAssignCommandPlaceholders(string code)
        {
            var p = new Regex(
                "AssignCommand\\s*\\(([^,]+),\\s*void\\s+\\w+\\s*=\\s*([^;]+);\\s*\\);",
                RegexOptions.Singleline);
            return p.Replace(code, m => "AssignCommand(" + m.Groups[1].Value.Trim() + ", " + m.Groups[2].Value.Trim() + ");");
        }

        private static string NormalizeTrailingZeroParams(string code)
        {
            var pattern = new Regex("([a-zA-Z_][a-zA-Z0-9_]*\\s*\\([^)]*),\\s*(0|0x0)\\s*\\)");
            var matches = new List<Tuple<int, int, string>>();
            foreach (Match m in pattern.Matches(code))
            {
                matches.Add(Tuple.Create(m.Index, m.Index + m.Length, m.Groups[1].Value));
            }

            string result = code;
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Tuple<int, int, string> match = matches[i];
                int start = match.Item1;
                int end = match.Item2;
                string group1 = match.Item3;
                result = result.Substring(0, start) + group1 + ")" + result.Substring(end);
            }

            return result;
        }

        private static string NormalizeIncludes(string code)
        {
            return Regex.Replace(code, "(?m)^\\s*#include[^\\n]*\\n?", string.Empty);
        }

        private static string NormalizeLeadingPlaceholders(string code)
        {
            string[] lines = code.Split('\n');
            int idx = 0;
            var placeholderPattern = new Regex("^[\\uFEFF]?int\\s+int\\d+\\s*=\\s*[-0-9xa-fA-F]+;");
            while (idx < lines.Length)
            {
                string line = lines[idx].Trim();
                if (line.StartsWith("//", StringComparison.Ordinal))
                {
                    idx++;
                    continue;
                }

                if (line.Length == 0)
                {
                    idx++;
                    continue;
                }

                if (placeholderPattern.IsMatch(line))
                {
                    idx++;
                    continue;
                }

                break;
            }

            if (idx == 0)
            {
                return code;
            }

            var sb = new StringBuilder();
            for (int i = idx; i < lines.Length; i++)
            {
                sb.Append(lines[i]);
                if (i < lines.Length - 1)
                {
                    sb.Append("\n");
                }
            }

            return sb.ToString();
        }

        private static string NormalizePlaceholderGlobals(string code)
        {
            string[] lines = code.Split('\n');
            int count = 0;
            int end = 0;
            var placeholderPattern = new Regex("^[\\uFEFF]?int\\s+(int\\d+|intGLOB_\\d+)\\s*=\\s*[-0-9xa-fA-F]+;");
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                if (placeholderPattern.IsMatch(line))
                {
                    count++;
                    end = i;
                }
                else if (line.Length == 0)
                {
                    continue;
                }
                else
                {
                    break;
                }
            }

            if (count >= 5 && end >= 0)
            {
                var sb = new StringBuilder();
                for (int i = end + 1; i < lines.Length; i++)
                {
                    sb.Append(lines[i]);
                    if (i < lines.Length - 1)
                    {
                        sb.Append("\n");
                    }
                }

                return sb.ToString();
            }

            return code;
        }

        private static string NormalizeFunctionBraces(string code)
        {
            string result = Regex.Replace(code, "\\)\\s*\\n\\s*\\{", ") {");
            const string R = "(?:\r\n|\n|\r)";
            var funcWithExtraBlock = new Regex(
                "(\\w+\\s+\\w+\\s*\\([^)]*\\)\\s*\\{\\s*" + R + ")([\t]+)\\{\\s*",
                RegexOptions.Multiline);
            string beforePattern1 = result;
            result = funcWithExtraBlock.Replace(result, "$1$2");
            bool pattern1Matched = !string.Equals(result, beforePattern1, StringComparison.Ordinal);

            if (!pattern1Matched)
            {
                var funcWithExtraBlockNewline = new Regex(
                    "(\\w+\\s+\\w+\\s*\\([^)]*\\)\\s*\\{\\s*\\n)([\t]+)\\{\\s*",
                    RegexOptions.Multiline);
                string beforePattern2 = result;
                result = funcWithExtraBlockNewline.Replace(result, "$1$2");
                pattern1Matched = !string.Equals(result, beforePattern2, StringComparison.Ordinal);
            }

            if (!pattern1Matched)
            {
                var funcWithExtraBlockSpaces = new Regex(
                    "(\\w+\\s+\\w+\\s*\\([^)]*\\)\\s*\\{\\s*" + R + ")([ ]+)\\{\\s*",
                    RegexOptions.Multiline);
                string beforePattern3 = result;
                result = funcWithExtraBlockSpaces.Replace(result, "$1$2");
                pattern1Matched = !string.Equals(result, beforePattern3, StringComparison.Ordinal);
            }

            if (!pattern1Matched && Regex.IsMatch(result, "\\{\\s*" + R + "[\\t ]*\\{"))
            {
                var funcWithExtraBlockMixed = new Regex(
                    "(\\w+\\s+\\w+\\s*\\([^)]*\\)\\s*\\{\\s*" + R + ")([\t ]*)\\{\\s*",
                    RegexOptions.Multiline);
                result = funcWithExtraBlockMixed.Replace(result, "$1$2");
            }

            result = Regex.Replace(
                result,
                "\\}\\s*\\n\\s*return\\s*;\\s*\\n\\s*\\}",
                "\nreturn; }",
                RegexOptions.Multiline);
            result = Regex.Replace(result, "\\}\\s+return\\s*;\\s*\\}", "return; }");
            return result;
        }

        private static string NormalizeEffectDeathDefaults(string code)
        {
            return Regex.Replace(code, "\\bEffectDeath\\s*\\(\\s*\\)", "EffectDeath(0, 1)");
        }

        private static string NormalizeReturnStatements(string code)
        {
            var pattern = new Regex("return\\s+\\(([^()]+(?:\\.[^()]*)*)\\);");
            string result = pattern.Replace(code, "return $1;");
            result = Regex.Replace(
                result,
                "([a-zA-Z_][a-zA-Z0-9_]*)\\s*=\\s*([^;\\n}]+);\\s*\\n\\s*return\\s*;",
                "return $2;",
                RegexOptions.Multiline);
            result = Regex.Replace(
                result,
                "([a-zA-Z_][a-zA-Z0-9_]*)\\s*=\\s*([^;}]+);\\s*\\}\\s*\\n\\s*return\\s*;",
                "return $2;",
                RegexOptions.Multiline);
            result = Regex.Replace(
                result,
                "([a-zA-Z_][a-zA-Z0-9_]*)\\s*=\\s*([^;]+);\\s+return\\s*;",
                "return $2;");
            return result;
        }

        private static string NormalizeComparisonParens(string code)
        {
            string result = code;
            var assignPattern = new Regex("(=)\\s*\\(([^;\\n]+?\\s*(==|!=|<=|>=|<|>)\\s*[^;\\n]+?)\\)\\s*;");
            result = assignPattern.Replace(result, m => m.Groups[1].Value + " " + m.Groups[2].Value.Trim() + ";");
            var generalPattern = new Regex("\\(([^()]+?\\s*(==|!=|<=|>=|<|>)\\s*[^()]+?)\\)");
            result = generalPattern.Replace(result, m => m.Groups[1].Value.Trim());
            return result;
        }

        private static string NormalizeAssignmentParens(string code)
        {
            var p = new Regex("(=)\\s*\\(([^;\\n]+)\\)\\s*;");
            return p.Replace(code, m => m.Groups[1].Value + " " + m.Groups[2].Value.Trim() + ";");
        }

        private static string NormalizeCallArgumentParens(string code)
        {
            var p = new Regex(",\\s*\\(([^(),]+)\\)");
            return p.Replace(code, m => ", " + m.Groups[1].Value.Trim());
        }

        private static string NormalizeStructNames(string code)
        {
            var p = new Regex("\\bstructtype(\\d+)\\b");
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            int counter = 1;
            return p.Replace(code, m =>
            {
                string orig = m.Value;
                if (!map.TryGetValue(orig, out string mapped))
                {
                    mapped = "structtype" + counter;
                    counter++;
                    map[orig] = mapped;
                }

                return mapped;
            });
        }

        private static string NormalizeSubroutineNames(string code)
        {
            var p = new Regex("\\bsub(\\d+)\\b");
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            int counter = 1;
            return p.Replace(code, m =>
            {
                string orig = m.Value;
                if (!map.TryGetValue(orig, out string mapped))
                {
                    mapped = "sub" + counter;
                    counter++;
                    map[orig] = mapped;
                }

                return mapped;
            });
        }

        private static string NormalizePrototypeDecls(string code)
        {
            return Regex.Replace(
                code,
                "(?m)^\\s*(int|float|void|string|object|location|vector|effect|talent)\\s+sub\\d+\\s*\\([^;]*\\);\\s*\\n?",
                string.Empty);
        }

        private static string NormalizeFunctionSignaturesByArity(string code)
        {
            var p = new Regex("(?m)^\\s*([A-Za-z_][\\w\\s\\*]*?)\\s+([A-Za-z_]\\w*)\\s*\\(([^)]*)\\)\\s*(\\{|;)");
            return p.Replace(code, m =>
            {
                string ret = m.Groups[1].Value.Trim();
                string name = m.Groups[2].Value.Trim();
                string @params = m.Groups[3].Value.Trim();
                int count = 0;
                if (@params.Length > 0)
                {
                    count = @params.Split(',').Length;
                }

                return ret + " " + name + "(/*params=" + count + "*/)" + m.Groups[4].Value;
            });
        }

        private static string NormalizeTrueFalse(string code)
        {
            string result = Regex.Replace(code, "\\bTRUE\\b", "1");
            result = Regex.Replace(result, "\\bFALSE\\b", "0");
            return result;
        }

        private static string NormalizeConstants(string code, Dictionary<string, string> constants)
        {
            if (constants == null || constants.Count == 0)
            {
                return code;
            }

            // Java: constants.keySet().stream().findFirst() (unordered first key; not sorted).
            string firstKey = constants.Keys.First();
            string prefixPattern = firstKey != null && firstKey.IndexOf('_') >= 0
                ? firstKey.Substring(0, firstKey.IndexOf('_') + 1)
                : "NPC_";
            // Java concatenates prefix into the pattern without escaping (safe for nwscript constant prefixes).
            var p = new Regex("\\b" + prefixPattern + "[A-Za-z0-9_]+\\b");
            return p.Replace(code, m =>
            {
                return constants.TryGetValue(m.Value, out string replacement) ? replacement : m.Value;
            });
        }

        private static string NormalizePlaceholderNames(string code)
        {
            return Regex.Replace(code, "__unknown_param_\\d+", "__unknown_param");
        }

        private static string NormalizeFunctionOrder(string code)
        {
            string[] lines = code.Split('\n');
            var functions = new List<string>();
            var current = new StringBuilder();
            int depth = 0;
            var preamble = new StringBuilder();
            var orphanedCode = new StringBuilder();
            bool inFunction = false;
            const string functionSignatureRegex = "^(\\s*\\w[\\w\\s\\*]+\\w\\s*\\([^)]*\\)\\s*\\{)";

            foreach (string line in lines)
            {
                bool isFunctionSignature = Regex.IsMatch(line, functionSignatureRegex);
                if (!inFunction && depth == 0 && !isFunctionSignature)
                {
                    preamble.AppendLine(line);
                    continue;
                }

                if (!inFunction && isFunctionSignature)
                {
                    inFunction = true;
                    current.Length = 0;
                }

                if (inFunction)
                {
                    current.AppendLine(line);
                    int openBraces = CountChar(line, '{');
                    int closeBraces = CountChar(line, '}');
                    depth += openBraces;
                    depth -= closeBraces;
                    if (openBraces > 0 && closeBraces > 0 && depth == 0)
                    {
                        functions.Add(current.ToString().Trim());
                        current.Length = 0;
                        inFunction = false;
                    }
                    else if (inFunction && depth == 0)
                    {
                        functions.Add(current.ToString().Trim());
                        current.Length = 0;
                        inFunction = false;
                    }
                }
                else if (!inFunction && depth == 0 && !isFunctionSignature && line.Trim().Length > 0)
                {
                    orphanedCode.AppendLine(line);
                }
            }

            if (current.Length > 0)
            {
                functions.Add(current.ToString().Trim());
            }

            functions.Sort(StringComparer.Ordinal);
            string preambleStr = preamble.ToString().Trim();
            string functionsStr = string.Join("\n", functions);
            string orphanedStr = orphanedCode.ToString().Trim();
            var rebuilt = new StringBuilder();
            if (preambleStr.Length > 0)
            {
                rebuilt.Append(preambleStr);
                if (functionsStr.Length > 0 || orphanedStr.Length > 0)
                {
                    rebuilt.Append("\n");
                }
            }

            if (functionsStr.Length > 0)
            {
                rebuilt.Append(functionsStr);
                if (orphanedStr.Length > 0)
                {
                    rebuilt.Append("\n");
                }
            }

            if (orphanedStr.Length > 0)
            {
                rebuilt.Append(orphanedStr);
            }

            return rebuilt.ToString().Trim();
        }

        private static int CountChar(string line, char ch)
        {
            int count = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ch)
                {
                    count++;
                }
            }

            return count;
        }

        private static string NormalizeBitwiseOperators(string code)
        {
            string result = Regex.Replace(code, "\\s*&\\s+(?!=)", " & ");
            result = Regex.Replace(result, "\\s*\\|\\s+(?!=)", " | ");
            return result;
        }

        private static string NormalizeLogicalOperators(string code)
        {
            string result = Regex.Replace(code, "\\&\\s*\\&", "&&");
            result = Regex.Replace(result, "\\|\\s*\\|", "||");
            result = result.Replace(" & & ", " && ");
            result = result.Replace(" | | ", " || ");
            return result;
        }

        private static string NormalizeIfSpacing(string code)
        {
            string result = Regex.Replace(code, "\\bif([a-zA-Z_][a-zA-Z0-9_]*)", "if $1");
            result = Regex.Replace(
                result,
                "\\bif\\s+([a-zA-Z_][a-zA-Z0-9_]*)\\s*(==|!=|<=|>=|<|>|&|\\|)",
                "if ($1 $2");
            result = Regex.Replace(result, "\\bif(?=[A-Za-z_])", "if ");
            result = Regex.Replace(result, "\\bwhile(?=[A-Za-z_])", "while ");
            result = Regex.Replace(result, "\\bfor(?=[A-Za-z_])", "for ");
            result = Regex.Replace(result, "\\bswitch(?=[A-Za-z_])", "switch ");
            return result;
        }

        private static string NormalizeDoubleParensInCalls(string code)
        {
            var p = new Regex("([A-Za-z_][A-Za-z0-9_]*)\\s*\\(\\(([^()]+)\\)(\\s*[),])");
            return p.Replace(code, m => m.Groups[1].Value + "(" + m.Groups[2].Value.Trim() + m.Groups[3].Value);
        }

        private static string NormalizeControlFlowConditions(string code)
        {
            var @out = new StringBuilder(code.Length);
            bool inString = false;
            for (int i = 0; i < code.Length; i++)
            {
                char ch = code[i];
                if (ch == '"' && (i == 0 || code[i - 1] != '\\'))
                {
                    inString = !inString;
                    @out.Append(ch);
                    continue;
                }

                if (!inString)
                {
                    string keyword = MatchControlKeyword(code, i);
                    if (keyword != null)
                    {
                        int kwEnd = i + keyword.Length;
                        int idx = kwEnd;
                        while (idx < code.Length && char.IsWhiteSpace(code[idx]))
                        {
                            idx++;
                        }

                        if (idx < code.Length && code[idx] == '(')
                        {
                            int endParen = FindMatchingParen(code, idx);
                            if (endParen != -1)
                            {
                                string condition = code.Substring(idx + 1, endParen - idx - 1);
                                condition = StripOuterParens(condition).Trim();
                                @out.Append(keyword).Append(" (").Append(condition).Append(")");
                                i = endParen;
                                continue;
                            }
                        }
                    }
                }

                @out.Append(ch);
            }

            return @out.ToString();
        }

        private static string MatchControlKeyword(string code, int index)
        {
            string[] keywords = { "if", "while", "switch", "for" };
            foreach (string kw in keywords)
            {
                int len = kw.Length;
                if (index + len <= code.Length && string.CompareOrdinal(code, index, kw, 0, len) == 0)
                {
                    char before = index == 0 ? '\0' : code[index - 1];
                    char after = index + len < code.Length ? code[index + len] : '\0';
                    if (!char.IsLetterOrDigit(before) && before != '_' &&
                        !char.IsLetterOrDigit(after) && after != '_')
                    {
                        return kw;
                    }
                }
            }

            return null;
        }

        private static int FindMatchingParen(string code, int openIdx)
        {
            int depth = 0;
            bool inString = false;
            for (int i = openIdx; i < code.Length; i++)
            {
                char c = code[i];
                if (c == '"' && (i == 0 || code[i - 1] != '\\'))
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                {
                    continue;
                }

                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static string StripOuterParens(string expr)
        {
            string result = expr.Trim();
            bool changed = true;
            while (changed && result.Length >= 2 && result[0] == '(' && result[result.Length - 1] == ')')
            {
                changed = false;
                int depth = 0;
                bool balanced = true;
                for (int i = 0; i < result.Length; i++)
                {
                    char c = result[i];
                    if (c == '(')
                    {
                        depth++;
                    }
                    else if (c == ')')
                    {
                        depth--;
                        if (depth == 0 && i < result.Length - 1)
                        {
                            balanced = false;
                            break;
                        }
                    }

                    if (depth < 0)
                    {
                        balanced = false;
                        break;
                    }
                }

                if (balanced && depth == 0)
                {
                    result = result.Substring(1, result.Length - 2).Trim();
                    changed = true;
                }
            }

            return result;
        }

        private static string NormalizeCommaSpacing(string code)
        {
            var @out = new StringBuilder(code.Length);
            bool inString = false;
            for (int i = 0; i < code.Length; i++)
            {
                char ch = code[i];
                if (ch == '"' && (i == 0 || code[i - 1] != '\\'))
                {
                    inString = !inString;
                    @out.Append(ch);
                    continue;
                }

                if (!inString && ch == ',')
                {
                    @out.Append(ch);
                    int j = i + 1;
                    while (j < code.Length)
                    {
                        char next = code[j];
                        if (next == ' ' || next == '\t' || next == '\r')
                        {
                            j++;
                            continue;
                        }

                        break;
                    }

                    i = j - 1;
                    continue;
                }

                @out.Append(ch);
            }

            return @out.ToString();
        }

        private static string NormalizeDeclarationAssignment(string code)
        {
            var declPattern = new Regex(
                "\\b(int|float|string|object|vector|location|effect|itemproperty|talent|action|event)\\s+([a-zA-Z_][a-zA-Z0-9_]*)\\s*;");
            var assignPattern = new Regex("\\b([a-zA-Z_][a-zA-Z0-9_]*)\\s*=\\s*(.+?);");
            string[] lines = code.Split('\n');
            var result = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                Match declMatcher = declPattern.Match(line);
                if (declMatcher.Success && i + 1 < lines.Length)
                {
                    string type = declMatcher.Groups[1].Value;
                    string varName = declMatcher.Groups[2].Value;
                    int nextLineIdx = i + 1;
                    while (nextLineIdx < lines.Length && lines[nextLineIdx].Trim().Length == 0)
                    {
                        nextLineIdx++;
                    }

                    if (nextLineIdx < lines.Length)
                    {
                        string nextLine = lines[nextLineIdx].Trim();
                        Match assignMatcher = assignPattern.Match(nextLine);
                        if (assignMatcher.Success && string.Equals(assignMatcher.Groups[1].Value, varName, StringComparison.Ordinal))
                        {
                            string value = assignMatcher.Groups[2].Value;
                            result.Append(type).Append(" ").Append(varName).Append(" = ").Append(value).Append(";\n");
                            i = nextLineIdx;
                            continue;
                        }
                    }
                }

                result.Append(lines[i]).Append("\n");
            }

            return result.ToString();
        }

        private static string NormalizeVariableNames(string code)
        {
            var varDeclPattern = new Regex(
                "\\b(int|float|string|object|vector|location|effect|itemproperty|talent|action|event)\\s+([a-zA-Z_][a-zA-Z0-9_]*)\\s*[=;]");
            var paramPattern = new Regex(
                "\\b(int|float|string|object|vector|location|effect|itemproperty|talent|action|event)\\s+([a-zA-Z_][a-zA-Z0-9_]*)\\s*[,)]");
            var functionPattern = new Regex("^(\\s*\\w[\\w\\s\\*]+\\w\\s*\\([^)]*\\)\\s*\\{)");
            string[] lines = code.Split(new[] { '\n' }, StringSplitOptions.None);
            var preambleLines = new List<string>();
            var functions = new List<FunctionBlock>();
            var currentFunction = new StringBuilder();
            int depth = 0;
            bool inFunction = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                bool isFunctionSignature = functionPattern.IsMatch(line);
                if (!inFunction && depth == 0 && !isFunctionSignature)
                {
                    preambleLines.Add(line);
                    continue;
                }

                if (!inFunction && isFunctionSignature)
                {
                    inFunction = true;
                    currentFunction.Length = 0;
                }

                if (inFunction)
                {
                    currentFunction.AppendLine(line);
                    int openBraces = CountChar(line, '{');
                    int closeBraces = CountChar(line, '}');
                    depth += openBraces;
                    depth -= closeBraces;
                    if (depth == 0)
                    {
                        functions.Add(new FunctionBlock(currentFunction.ToString()));
                        currentFunction.Length = 0;
                        inFunction = false;
                    }
                }
            }

            var result = new StringBuilder();
            foreach (string preambleLine in preambleLines)
            {
                result.AppendLine(preambleLine);
            }

            foreach (FunctionBlock func in functions)
            {
                string funcCode = func.Content;
                var varMap = new Dictionary<string, string>(StringComparer.Ordinal);
                var typeCounters = new Dictionary<string, int>(StringComparer.Ordinal);
                var varOrder = new List<string>();
                foreach (Match paramMatcher in paramPattern.Matches(funcCode))
                {
                    string type = paramMatcher.Groups[1].Value;
                    string varName = paramMatcher.Groups[2].Value;
                    if (Regex.IsMatch(varName, "^(int|float|string|object|vector|location|effect|itemproperty|talent|action|event)\\d+$"))
                    {
                        continue;
                    }

                    if (IsReservedName(varName))
                    {
                        continue;
                    }

                    if (varMap.ContainsKey(varName))
                    {
                        continue;
                    }

                    string canonicalType = type.ToLowerInvariant();
                    int counter = typeCounters.ContainsKey(canonicalType) ? typeCounters[canonicalType] + 1 : 1;
                    typeCounters[canonicalType] = counter;
                    string canonicalName = canonicalType + counter;
                    varMap[varName] = canonicalName;
                    varOrder.Add(varName);
                }

                foreach (Match matcher in varDeclPattern.Matches(funcCode))
                {
                    string type = matcher.Groups[1].Value;
                    string varName = matcher.Groups[2].Value;
                    if (Regex.IsMatch(varName, "^(int|float|string|object|vector|location|effect|itemproperty|talent|action|event)\\d+$"))
                    {
                        continue;
                    }

                    if (IsReservedName(varName))
                    {
                        continue;
                    }

                    if (varMap.ContainsKey(varName))
                    {
                        continue;
                    }

                    string canonicalType = type.ToLowerInvariant();
                    int counter = typeCounters.ContainsKey(canonicalType) ? typeCounters[canonicalType] + 1 : 1;
                    typeCounters[canonicalType] = counter;
                    string canonicalName = canonicalType + counter;
                    varMap[varName] = canonicalName;
                    varOrder.Add(varName);
                }

                string normalizedFunc = funcCode;
                for (int i = varOrder.Count - 1; i >= 0; i--)
                {
                    string originalName = varOrder[i];
                    string canonicalName = varMap[originalName];
                    normalizedFunc = Regex.Replace(
                        normalizedFunc,
                        "\\b" + Regex.Escape(originalName) + "\\b",
                        canonicalName);
                }

                result.Append(normalizedFunc);
            }

            return result.ToString();
        }

        private sealed class FunctionBlock
        {
            public FunctionBlock(string content)
            {
                Content = content;
            }

            public string Content { get; }
        }

        private static bool IsReservedName(string name)
        {
            string[] reserved =
            {
                "int", "float", "string", "object", "void", "vector", "location",
                "effect", "itemproperty", "talent", "action", "event", "struct",
                "if", "else", "for", "while", "do", "switch", "case", "default",
                "return", "break", "continue", "main", "StartingConditional",
                "GetGlobalNumber", "GetGlobalBoolean", "GetGlobalString",
                "SetGlobalNumber", "SetGlobalBoolean", "SetGlobalString",
                "GetObjectByTag", "GetPartyMemberByIndex", "SetPartyLeader",
                "NoClicksFor", "DelayCommand", "SignalEvent", "EventUserDefined",
                "OBJECT_SELF", "GetLastOpenedBy", "IsObjectPartyMember"
            };

            foreach (string reservedName in reserved)
            {
                if (string.Equals(reservedName, name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string StripComments(string code)
        {
            var result = new StringBuilder();
            bool inBlockComment = false;
            bool inString = false;
            char[] chars = code.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (inBlockComment)
                {
                    if (i < chars.Length - 1 && chars[i] == '*' && chars[i + 1] == '/')
                    {
                        inBlockComment = false;
                        i++;
                    }

                    continue;
                }

                if (inString)
                {
                    result.Append(chars[i]);
                    if (chars[i] == '"' && (i == 0 || chars[i - 1] != '\\'))
                    {
                        inString = false;
                    }

                    continue;
                }

                if (chars[i] == '"')
                {
                    inString = true;
                    result.Append(chars[i]);
                }
                else if (i < chars.Length - 1 && chars[i] == '/' && chars[i + 1] == '/')
                {
                    while (i < chars.Length && chars[i] != '\n')
                    {
                        i++;
                    }

                    if (i < chars.Length)
                    {
                        result.Append('\n');
                    }
                }
                else if (i < chars.Length - 1 && chars[i] == '/' && chars[i + 1] == '*')
                {
                    inBlockComment = true;
                    i++;
                }
                else
                {
                    result.Append(chars[i]);
                }
            }

            return result.ToString();
        }

        // --- merged from NCSDecompCliRoundTripTest.PortDiffBytecode.cs ---
        private enum DiffLineType
        {
            Context,
            Removed,
            Added
        }

        private sealed class DiffLine
        {
            public DiffLine(DiffLineType type, string content)
            {
                Type = type;
                Content = content;
            }

            public DiffLineType Type { get; }
            public string Content { get; }
        }

        private sealed class DiffResult
        {
            public readonly List<DiffLine> Lines = new List<DiffLine>();

            public bool IsEmpty()
            {
                for (int i = 0; i < Lines.Count; i++)
                {
                    if (Lines[i].Type != DiffLineType.Context)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private static string FormatUnifiedDiff(string expected, string actual)
        {
            string[] expectedLines = expected.Split(new[] { '\n' }, StringSplitOptions.None);
            string[] actualLines = actual.Split(new[] { '\n' }, StringSplitOptions.None);
            DiffResult diffResult = ComputeDiff(expectedLines, actualLines);
            if (diffResult.IsEmpty())
            {
                return null;
            }

            var diff = new StringBuilder();
            diff.Append("    --- expected\n");
            diff.Append("    +++ actual\n");

            int oldLineNum = 1;
            int newLineNum = 1;
            int firstOldLine = -1;
            int firstNewLine = -1;
            int lastOldLine = -1;
            int lastNewLine = -1;

            foreach (DiffLine line in diffResult.Lines)
            {
                if (line.Type == DiffLineType.Removed)
                {
                    if (firstOldLine == -1)
                    {
                        firstOldLine = oldLineNum;
                    }

                    lastOldLine = oldLineNum;
                    oldLineNum++;
                }
                else if (line.Type == DiffLineType.Added)
                {
                    if (firstNewLine == -1)
                    {
                        firstNewLine = newLineNum;
                    }

                    lastNewLine = newLineNum;
                    newLineNum++;
                }
                else
                {
                    if (firstOldLine != -1 && lastOldLine == oldLineNum - 1)
                    {
                        lastOldLine = oldLineNum;
                    }

                    if (firstNewLine != -1 && lastNewLine == newLineNum - 1)
                    {
                        lastNewLine = newLineNum;
                    }

                    oldLineNum++;
                    newLineNum++;
                }
            }

            int oldStart;
            int oldCount;
            int newStart;
            int newCount;
            if (firstOldLine == -1)
            {
                oldStart = 1;
                oldCount = expectedLines.Length;
            }
            else
            {
                oldStart = firstOldLine;
                oldCount = lastOldLine - firstOldLine + 1;
            }

            if (firstNewLine == -1)
            {
                newStart = 1;
                newCount = actualLines.Length;
            }
            else
            {
                newStart = firstNewLine;
                newCount = lastNewLine - firstNewLine + 1;
            }

            diff.Append("    @@ -").Append(oldStart);
            if (oldCount != 1)
            {
                diff.Append(",").Append(oldCount);
            }

            diff.Append(" +").Append(newStart);
            if (newCount != 1)
            {
                diff.Append(",").Append(newCount);
            }

            diff.Append(" @@\n");

            foreach (DiffLine line in diffResult.Lines)
            {
                switch (line.Type)
                {
                    case DiffLineType.Context:
                        diff.Append("     ").Append(line.Content).Append("\n");
                        break;
                    case DiffLineType.Removed:
                        diff.Append("    -").Append(line.Content).Append("\n");
                        break;
                    case DiffLineType.Added:
                        diff.Append("    +").Append(line.Content).Append("\n");
                        break;
                }
            }

            return diff.ToString();
        }

        private static DiffResult ComputeDiff(string[] expected, string[] actual)
        {
            var result = new DiffResult();
            int m = expected.Length;
            int n = actual.Length;
            var dp = new int[m + 1, n + 1];
            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    if (string.Equals(expected[i - 1], actual[j - 1], StringComparison.Ordinal))
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                    }
                }
            }

            int ii = m;
            int jj = n;
            var tempLines = new List<DiffLine>();
            while (ii > 0 || jj > 0)
            {
                if (ii > 0 && jj > 0 && string.Equals(expected[ii - 1], actual[jj - 1], StringComparison.Ordinal))
                {
                    tempLines.Add(new DiffLine(DiffLineType.Context, expected[ii - 1]));
                    ii--;
                    jj--;
                }
                else if (jj > 0 && (ii == 0 || dp[ii, jj - 1] >= dp[ii - 1, jj]))
                {
                    tempLines.Add(new DiffLine(DiffLineType.Added, actual[jj - 1]));
                    jj--;
                }
                else if (ii > 0)
                {
                    tempLines.Add(new DiffLine(DiffLineType.Removed, expected[ii - 1]));
                    ii--;
                }
            }

            for (int k = tempLines.Count - 1; k >= 0; k--)
            {
                result.Lines.Add(tempLines[k]);
            }

            return result;
        }

        private static void AssertBytecodeEqual(
            string originalNcs,
            string roundTripNcs,
            string gameFlag,
            string displayName,
            string contextDecompiledNssPath = null)
        {
            BytecodeDiffResult diff = FindBytecodeDiff(originalNcs, roundTripNcs);
            if (diff == null)
            {
                return;
            }

            string originalAction = GetActionNameAtOffset(originalNcs, diff.Offset, gameFlag);
            string roundTripAction = GetActionNameAtOffset(roundTripNcs, diff.Offset, gameFlag);

            if (!string.IsNullOrWhiteSpace(contextDecompiledNssPath) && !File.Exists(contextDecompiledNssPath))
            {
                throw new IOException(
                    "Decompiled NSS is required for bytecode mismatch diagnostics but was not found: " + DisplayPath(contextDecompiledNssPath));
            }

            string decompiledNss = contextDecompiledNssPath;
            bool decompiledExists = !string.IsNullOrEmpty(decompiledNss) && File.Exists(decompiledNss);

            var message = new StringBuilder();
            message.Append("═══════════════════════════════════════════════════════════════\n");
            message.Append("BYTECODE MISMATCH: ").Append(displayName).Append("\n");
            message.Append("═══════════════════════════════════════════════════════════════\n\n");
            message.Append("LOCATION:\n");
            message.Append("  Offset: ").Append(diff.Offset).Append(" (0x").Append(diff.Offset.ToString("x", System.Globalization.CultureInfo.InvariantCulture)).Append(")\n");
            message.Append("  Original: ").Append(FormatByteValue(diff.OriginalByte));
            if (originalAction != null)
            {
                message.Append(" → ").Append(originalAction);
            }

            message.Append("\n");
            message.Append("  Round-trip: ").Append(FormatByteValue(diff.RoundTripByte));
            if (roundTripAction != null)
            {
                message.Append(" → ").Append(roundTripAction);
            }

            message.Append("\n\nFILES:\n");
            message.Append("  Original NCS: ").Append(originalNcs).Append("\n");
            message.Append("  Round-trip NCS: ").Append(roundTripNcs).Append("\n");
            if (decompiledExists)
            {
                message.Append("  Decompiled NSS: ").Append(decompiledNss).Append("\n");
            }

            message.Append("\nBYTECODE CONTEXT:\n");
            message.Append("  Original:  ").Append(diff.OriginalContext).Append("\n");
            message.Append("  Round-trip: ").Append(diff.RoundTripContext).Append("\n\n");
            message.Append("FILE SIZES:\n");
            message.Append("  Original: ").Append(diff.OriginalLength).Append(" bytes\n");
            message.Append("  Round-trip: ").Append(diff.RoundTripLength).Append(" bytes\n");

            string pcodeDiff = DiffPcodeListings(originalNcs, roundTripNcs, gameFlag);
            if (!string.IsNullOrWhiteSpace(pcodeDiff))
            {
                message.Append("\nP-CODE DIFF (first 50 lines):\n");
                string[] lines = pcodeDiff.Split(new[] { '\n' }, StringSplitOptions.None);
                int showLines = Math.Min(50, lines.Length);
                for (int i = 0; i < showLines; i++)
                {
                    message.Append(lines[i]).Append("\n");
                }

                if (lines.Length > 50)
                {
                    message.Append("... (").Append(lines.Length - 50).Append(" more lines)\n");
                }
            }

            if (decompiledExists)
            {
                try
                {
                    string decompiledContent = File.ReadAllText(decompiledNss, Encoding.UTF8);
                    string[] decompiledLines = decompiledContent.Split(new[] { '\n' }, StringSplitOptions.None);
                    message.Append("\nDECOMPILED OUTPUT (first 30 lines):\n");
                    int showLines = Math.Min(30, decompiledLines.Length);
                    for (int i = 0; i < showLines; i++)
                    {
                        message.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0,4}: {1}\n", i + 1, decompiledLines[i]);
                    }

                    if (decompiledLines.Length > 30)
                    {
                        message.Append("... (").Append(decompiledLines.Length - 30).Append(" more lines)\n");
                    }
                }
                catch (Exception e)
                {
                    message.Append("\n(Unable to read decompiled file: ").Append(e.Message).Append(")\n");
                }
            }

            message.Append("\n═══════════════════════════════════════════════════════════════\n");
            throw new InvalidOperationException(message.ToString());
        }

        private static string GetActionNameAtOffset(string ncsFile, long offset, string gameFlag)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(ncsFile);
                int offsetInt = (int)offset;
                if (offsetInt < 0 || offsetInt >= bytes.Length)
                {
                    return null;
                }

                if (offsetInt > 0 && (bytes[offsetInt] & 0xFF) < 200)
                {
                    int actionId = bytes[offsetInt] & 0xFF;
                    string nwscriptPath = string.Equals(gameFlag, "k1", StringComparison.Ordinal) ? K1Nwscript : K2Nwscript;
                    if (File.Exists(nwscriptPath))
                    {
                        string content = File.ReadAllText(nwscriptPath, Encoding.UTF8);
                        string pattern = "// " + actionId + ":";
                        int idx = content.IndexOf(pattern, StringComparison.Ordinal);
                        if (idx >= 0)
                        {
                            int searchStart = idx + pattern.Length;
                            int searchEnd = Math.Min(searchStart + 500, content.Length);
                            string section = content.Substring(searchStart, searchEnd - searchStart);
                            var funcPattern = new Regex(
                                "\\b(void|int|float|string|object|location|effect|talent|action|itemproperty|vector)\\s+(\\w+)\\s*\\(");
                            Match matcher = funcPattern.Match(section);
                            if (matcher.Success)
                            {
                                return matcher.Groups[2].Value;
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private static BytecodeDiffResult FindBytecodeDiff(string originalNcs, string roundTripNcs)
        {
            byte[] originalBytes = File.ReadAllBytes(originalNcs);
            byte[] roundTripBytes = File.ReadAllBytes(roundTripNcs);
            int maxLength = Math.Max(originalBytes.Length, roundTripBytes.Length);
            for (int i = 0; i < maxLength; i++)
            {
                int original = i < originalBytes.Length ? originalBytes[i] & 0xFF : -1;
                int roundTrip = i < roundTripBytes.Length ? roundTripBytes[i] & 0xFF : -1;
                if (original != roundTrip)
                {
                    return new BytecodeDiffResult
                    {
                        Offset = i,
                        OriginalByte = original,
                        RoundTripByte = roundTrip,
                        OriginalLength = originalBytes.Length,
                        RoundTripLength = roundTripBytes.Length,
                        OriginalContext = RenderHexContext(originalBytes, i),
                        RoundTripContext = RenderHexContext(roundTripBytes, i)
                    };
                }
            }

            return null;
        }

        private static string RenderHexContext(byte[] bytes, int focus)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return "<empty>";
            }

            int anchor = Math.Min(Math.Max(focus, 0), bytes.Length - 1);
            int start = Math.Max(0, anchor - 8);
            int end = Math.Min(bytes.Length, anchor + 9);
            var sb = new StringBuilder();
            for (int i = start; i < end; i++)
            {
                if (i > start)
                {
                    sb.Append(' ');
                }

                sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:X2}", bytes[i]);
                if (i == focus)
                {
                    sb.Append('*');
                }
            }

            return sb.ToString();
        }

        private static string FormatByteValue(int value)
        {
            if (value < 0)
            {
                return "<EOF>";
            }

            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "0x{0:X2} ({1})", value, value);
        }

        private static string DiffPcodeListings(string originalNcs, string roundTripNcs, string gameFlag)
        {
            string tempDir = null;
            try
            {
                Directory.CreateDirectory(CompileTempRoot);
                tempDir = Path.Combine(CompileTempRoot, "pcode_diff_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(tempDir);
                string originalPcode = Path.Combine(tempDir, "original.pcode");
                string roundTripPcode = Path.Combine(tempDir, "roundtrip.pcode");

                DecompileNcsToPcode(originalNcs, originalPcode, gameFlag);
                DecompileNcsToPcode(roundTripNcs, roundTripPcode, gameFlag);

                string expected = File.ReadAllText(originalPcode, Encoding.UTF8);
                string actual = File.ReadAllText(roundTripPcode, Encoding.UTF8);
                return FormatUnifiedDiff(expected, actual);
            }
            catch (Exception e)
            {
                return "Failed to generate p-code diff: " + e.Message;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempDir))
                {
                    try
                    {
                        DeleteDirectory(tempDir);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        private static void DecompileNcsToPcode(string ncsPath, string outputPcode, string gameFlag)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPcode) ?? ".");
            bool isK2 = string.Equals(gameFlag, "k2", StringComparison.Ordinal);
            var config = new NCSDecomp.Core.NwnnsscompConfig(NwnCompiler, ncsPath, outputPcode, isK2);
            string[] cmd = config.GetDecompileArgs(Path.GetFullPath(NwnCompiler));
            RunProcessWithTimeout(cmd, Path.GetDirectoryName(ncsPath) ?? ".", "Decompile to p-code for " + DisplayPath(ncsPath));
            if (!File.Exists(outputPcode))
            {
                throw new IOException("P-code output missing at: " + DisplayPath(outputPcode));
            }
        }

        private sealed class BytecodeDiffResult
        {
            public long Offset { get; set; }
            public int OriginalByte { get; set; }
            public int RoundTripByte { get; set; }
            public long OriginalLength { get; set; }
            public long RoundTripLength { get; set; }
            public string OriginalContext { get; set; }
            public string RoundTripContext { get; set; }
        }

        // --- merged from NCSDecompCliRoundTripTest.PortJavaForbiddenStubs.cs ---
        /// <summary>Java <c>declareMissingVariables</c> — intentionally unusable; fixes belong in the decompiler.</summary>
        private static string DeclareMissingVariables(string content)
        {
            throw new NotSupportedException(
                "Variable declaration fixing is FORBIDDEN. Fix the decompiler source code instead.");
        }

        /// <summary>Java <c>fixFunctionSignaturesFromCallSites</c> — intentionally unusable.</summary>
        private static string FixFunctionSignaturesFromCallSites(string content, string gameFlag)
        {
            throw new NotSupportedException(
                "Function signature fixing is FORBIDDEN. Fix the decompiler source code instead.");
        }

        /// <summary>Java <c>fixReturnTypeMismatches</c> — intentionally unusable.</summary>
        private static string FixReturnTypeMismatches(string content)
        {
            throw new NotSupportedException(
                "Return type mismatch fixing is FORBIDDEN. Fix the decompiler source code instead.");
        }

        // --- merged from NCSDecompCliRoundTripTest.PortJavaLegacyRemoved.cs ---
        /// <summary>Java <c>loadNwscriptSignatures</c>.</summary>
        private static Dictionary<string, string[]> LoadNwscriptSignatures(string gameFlag)
        {
            var signatures = new Dictionary<string, string[]>(StringComparer.Ordinal);
            try
            {
                string nwscriptPath = string.Equals(gameFlag, "k1", StringComparison.Ordinal)
                    ? K1Nwscript
                    : K2Nwscript;
                if (File.Exists(nwscriptPath))
                {
                    string content = File.ReadAllText(nwscriptPath, Encoding.UTF8);
                    var sigPattern = new Regex(
                        "(int|void|float|string|object|vector|location|effect|itemproperty|talent|action|event)\\s+([a-zA-Z_][a-zA-Z0-9_]*)\\s*\\(([^)]*)\\)\\s*;");
                    foreach (Match sigMatcher in sigPattern.Matches(content))
                    {
                        string funcName = sigMatcher.Groups[2].Value;
                        string paramList = sigMatcher.Groups[3].Value.Trim();
                        if (paramList.Length > 0)
                        {
                            string[] paramTypes = paramList.Split(',')
                                .Select(p =>
                                {
                                    p = p.Trim();
                                    string[] parts = Regex.Split(p, "\\s+");
                                    return parts.Length > 0 ? parts[0] : "int";
                                })
                                .ToArray();
                            signatures[funcName] = paramTypes;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors loading signatures
            }

            return signatures;
        }

        /// <summary>Java <c>extractParamName</c>.</summary>
        private static string ExtractParamName(string paramDecl)
        {
            paramDecl = paramDecl.Trim();
            string[] parts = Regex.Split(paramDecl, "\\s+");
            return parts.Length > 1 ? parts[parts.Length - 1] : paramDecl;
        }

        /// <summary>Java <c>findFunctionBody</c>.</summary>
        private static string FindFunctionBody(string content, int funcStartPos)
        {
            int bracePos = content.IndexOf('{', funcStartPos);
            if (bracePos < 0)
            {
                return null;
            }

            int start = bracePos + 1;
            int depth = 1;
            int i = start;
            while (i < content.Length && depth > 0)
            {
                char c = content[i];
                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                }

                i++;
            }

            if (depth == 0)
            {
                return content.Substring(start, i - 1 - start);
            }

            return null;
        }

        /// <summary>Java <c>inferTypeFromArgument</c>.</summary>
        private static string InferTypeFromArgument(string arg)
        {
            arg = arg.Trim();
            if (arg.Length >= 2 && arg.StartsWith("\"", StringComparison.Ordinal) && arg.EndsWith("\"", StringComparison.Ordinal))
            {
                return "string";
            }

            if (Regex.IsMatch(arg, "^-?\\d+\\.\\d+[fF]?$") || Regex.IsMatch(arg, "^-?\\d+\\.\\d+$"))
            {
                return "float";
            }

            if (Regex.IsMatch(arg, "^-?\\d+$"))
            {
                return null;
            }

            return null;
        }

        private static List<string> SplitCallArgsTopLevel(string args)
        {
            var argList = new List<string>();
            int depth = 0;
            var currentArg = new StringBuilder();
            for (int idx = 0; idx < args.Length; idx++)
            {
                char c = args[idx];
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    argList.Add(currentArg.ToString().Trim());
                    currentArg.Length = 0;
                    continue;
                }

                currentArg.Append(c);
            }

            if (currentArg.Length > 0)
            {
                argList.Add(currentArg.ToString().Trim());
            }

            return argList;
        }

        /// <summary>Java <c>fixFunctionSignaturesFromCallSites_REMOVED</c> (legacy; do not use).</summary>
        private static string FixFunctionSignaturesFromCallSitesRemoved(string content, string gameFlag)
        {
            Dictionary<string, string[]> nwscriptSigs = LoadNwscriptSignatures(gameFlag);
            var funcDefPattern = new Regex(
                "(void|int|float|string|object|vector|location|effect|itemproperty|talent|action|event)\\s+([a-zA-Z_][a-zA-Z0-9_]*)\\s*\\(([^)]*)\\)");

            var funcBodies = new Dictionary<string, string>(StringComparer.Ordinal);
            var funcSignatures = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match funcMatcher in funcDefPattern.Matches(content))
            {
                string funcName = funcMatcher.Groups[2].Value;
                string fullMatch = funcMatcher.Value;
                int matchStart = funcMatcher.Index;
                int matchEnd = funcMatcher.Index + funcMatcher.Length;

                int bracePos = content.IndexOf('{', matchEnd);
                if (bracePos != -1 && (bracePos - matchEnd) < 50)
                {
                    string funcBody = FindFunctionBody(content, matchStart);
                    if (funcBody != null)
                    {
                        funcBodies[funcName] = funcBody;
                        funcSignatures[funcName] = fullMatch;
                    }
                }
            }

            var result = new StringBuilder();
            int lastAppend = 0;
            foreach (Match funcMatcher in funcDefPattern.Matches(content))
            {
                string funcName = funcMatcher.Groups[2].Value;
                string paramListRaw = funcMatcher.Groups[3].Value;
                string returnType = funcMatcher.Groups[1].Value;

                if (string.IsNullOrWhiteSpace(paramListRaw))
                {
                    result.Append(content, lastAppend, funcMatcher.Index - lastAppend);
                    result.Append(funcMatcher.Value);
                    lastAppend = funcMatcher.Index + funcMatcher.Length;
                    continue;
                }

                string[] paramDecls = paramListRaw.Split(',');
                var typeHints = new Dictionary<int, string>();

                if (funcBodies.TryGetValue(funcName, out string funcBody))
                {
                    var paramNames = new string[paramDecls.Length];
                    for (int i = 0; i < paramDecls.Length; i++)
                    {
                        paramNames[i] = ExtractParamName(paramDecls[i].Trim());
                    }

                    foreach (KeyValuePair<string, string[]> nwscriptEntry in nwscriptSigs)
                    {
                        string nwscriptFunc = nwscriptEntry.Key;
                        string[] expectedTypes = nwscriptEntry.Value;
                        var nwscriptCallPattern = new Regex(Regex.Escape(nwscriptFunc) + "\\s*\\(([^)]*)\\)");
                        foreach (Match nwscriptCallMatcher in nwscriptCallPattern.Matches(funcBody))
                        {
                            string args = nwscriptCallMatcher.Groups[1].Value;
                            List<string> argList = SplitCallArgsTopLevel(args);

                            for (int i = 0; i < argList.Count && i < expectedTypes.Length; i++)
                            {
                                string arg = argList[i];
                                string expectedType = expectedTypes[i];

                                for (int j = 0; j < paramNames.Length; j++)
                                {
                                    string paramName = paramNames[j];
                                    bool isParam = arg.Equals(paramName, StringComparison.Ordinal);
                                    if (!isParam)
                                    {
                                        var paramPattern = new Regex("\\b" + Regex.Escape(paramName) + "\\b");
                                        isParam = paramPattern.IsMatch(arg);
                                    }

                                    if (isParam)
                                    {
                                        if (expectedType != null && !string.Equals(expectedType, "int", StringComparison.Ordinal) &&
                                            paramDecls[j].Trim().StartsWith("int ", StringComparison.Ordinal))
                                        {
                                            typeHints[j] = expectedType;
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                var callPattern = new Regex(Regex.Escape(funcName) + "\\s*\\(([^)]*)\\)");
                foreach (Match callMatcher in callPattern.Matches(content))
                {
                    if (callMatcher.Index >= funcMatcher.Index && callMatcher.Index < funcMatcher.Index + funcMatcher.Length)
                    {
                        continue;
                    }

                    string args = callMatcher.Groups[1].Value;
                    string[] argList = args.Split(',');
                    if (argList.Length == paramDecls.Length)
                    {
                        for (int i = 0; i < argList.Length; i++)
                        {
                            string callArg = argList[i].Trim();
                            string inferredType = InferTypeFromArgument(callArg);
                            if (inferredType != null && paramDecls[i].Trim().StartsWith("int ", StringComparison.Ordinal))
                            {
                                typeHints[i] = inferredType;
                            }
                        }
                    }
                }

                if (typeHints.Count > 0)
                {
                    var newParams = new StringBuilder();
                    for (int i = 0; i < paramDecls.Length; i++)
                    {
                        if (i > 0)
                        {
                            newParams.Append(", ");
                        }

                        string paramDecl = paramDecls[i].Trim();
                        if (typeHints.TryGetValue(i, out string hintType) &&
                            paramDecl.StartsWith("int ", StringComparison.Ordinal))
                        {
                            paramDecl = Regex.Replace(paramDecl, "^int\\s+", hintType + " ");
                        }

                        newParams.Append(paramDecl);
                    }

                    string replacement = returnType + " " + funcName + "(" + newParams + ")";
                    result.Append(content, lastAppend, funcMatcher.Index - lastAppend);
                    result.Append(replacement);
                    lastAppend = funcMatcher.Index + funcMatcher.Length;
                }
                else
                {
                    result.Append(content, lastAppend, funcMatcher.Index - lastAppend);
                    result.Append(funcMatcher.Value);
                    lastAppend = funcMatcher.Index + funcMatcher.Length;
                }
            }

            result.Append(content, lastAppend, content.Length - lastAppend);
            string fixedContent = result.ToString();

            var funcDefs = new Dictionary<string, string>(StringComparer.Ordinal);
            var funcProtos = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match funcMatcher in funcDefPattern.Matches(fixedContent))
            {
                string funcName = funcMatcher.Groups[2].Value;
                string fullSig = funcMatcher.Value;
                int matchEnd = funcMatcher.Index + funcMatcher.Length;

                int semicolonPos = fixedContent.IndexOf(';', matchEnd);
                int bracePos = fixedContent.IndexOf('{', matchEnd);

                if (semicolonPos != -1 && (bracePos == -1 || semicolonPos < bracePos))
                {
                    funcProtos[funcName] = fullSig;
                }
                else if (bracePos != -1 && (semicolonPos == -1 || bracePos < semicolonPos))
                {
                    funcDefs[funcName] = fullSig;
                }
            }

            foreach (KeyValuePair<string, string> entry in funcDefs)
            {
                string funcName = entry.Key;
                string defSig = entry.Value;
                if (funcProtos.TryGetValue(funcName, out string protoSig) && !string.Equals(protoSig, defSig, StringComparison.Ordinal))
                {
                    string newProto = defSig + ";";
                    fixedContent = fixedContent.Replace(protoSig + ";", newProto, StringComparison.Ordinal);
                }
            }

            return fixedContent;
        }

        /// <summary>Java <c>fixReturnTypeMismatches_REMOVED</c> (legacy; do not use).</summary>
        private static string FixReturnTypeMismatchesRemoved(string content)
        {
            var voidFuncPattern = new Regex("void\\s+([a-zA-Z_][a-zA-Z0-9_]*)\\s*\\([^)]*\\)\\s*\\{");
            var result = new StringBuilder();
            int lastPos = 0;
            foreach (Match funcMatcher in voidFuncPattern.Matches(content))
            {
                int funcStart = funcMatcher.Index;
                int funcEnd = funcMatcher.Index + funcMatcher.Length;

                if (lastPos > funcStart)
                {
                    continue;
                }

                if (lastPos < funcStart)
                {
                    result.Append(content, lastPos, funcStart - lastPos);
                }

                result.Append(funcMatcher.Value);

                string funcBody = FindFunctionBody(content, funcStart);
                if (funcBody != null)
                {
                    string fixedBody = Regex.Replace(funcBody, "return\\s+[^;]+;", "return;");
                    result.Append(fixedBody);
                    result.Append('}');
                    lastPos = Math.Min(content.Length, funcEnd + funcBody.Length + 1);
                }
                else
                {
                    lastPos = funcEnd;
                }
            }

            if (lastPos < content.Length)
            {
                result.Append(content, lastPos, content.Length - lastPos);
            }

            return result.ToString();
        }
    }
}
