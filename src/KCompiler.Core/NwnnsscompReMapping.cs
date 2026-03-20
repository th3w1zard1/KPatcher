namespace KCompiler
{
    /// <summary>
    /// Reverse-engineering mapping for nwnnsscomp.exe (Ghidra/agdec-mcp).
    /// Binary: nwnnsscomp.exe (x86:LE:32:default, windows). Import via agdec: open with path to .gzf or use import-binary.
    /// Compile logic in src: <see cref="ManagedNwnnsscomp"/> and <see cref="Cli.NwnnsscompCliParser"/> (this assembly).
    /// </summary>
    public static class NwnnsscompReMapping
    {
        /// <summary>Entry point (export).</summary>
        public const string EntrySymbol = "entry";
        public const uint EntryAddress = 0x0041e6e4u;

        /// <summary>Main CLI / message loop. Called from entry with (argc, argv, env). Returns exit code.</summary>
        public const string MainCliSymbol = "FUN_004032da";
        public const uint MainCliAddress = 0x004032dau;

        /// <summary>Single-file compile: opens input path, compiles to output. Called when DAT_00433e00 == 0 (compile mode) with one or more positionals.</summary>
        public const uint CompileSingleFileAddress = 0x00403075u;

        /// <summary>Open input file (used in decompile path).</summary>
        public const uint OpenInputFileAddress = 0x00402b64u;

        /// <summary>Option character values from FUN_004032da switch (FUN_0041e33d).</summary>
        public const int OptionCompile = 0x63;   // 'c' -> DAT_00433050 = 1
        public const int OptionDecompile = 0x64;  // 'd' -> DAT_00433050 = 0
        public const int OptionExtra = 0x65;      // 'e' -> DAT_00433e02 = 1 (e.g. enable debug)
        public const int OptionOutput = 0x6f;    // 'o' -> next arg is outfile

        /// <summary>Usage string (from binary). Matches KCompiler.NET / NwnnsscompCliParser for -c, -o; KCompiler extends with -g, --outputdir, --debug, --nwscript.</summary>
        public const string Usage =
            "nwnnsscomp [-cd] infile [-o outfile]\n" +
            "  infile - name of the input file.\n" +
            "  outfile - name of the output file.\n" +
            "  -c - Compile the script (default)\n" +
            "  -d - Decompile the script (can't be used with -c)\n" +
            "Note: this version of nwnnsscomp requires the nwscript.nss to be\n" +
            "located in the same directory as nwnnsscomp.  For compilations,\n" +
            "include scripts should be in the same directory as the the target\n" +
            "source files.  (Non-Bioware extensions and v1.00 compilations implied.)";

        /// <summary>Banner (from binary).</summary>
        public const string Banner =
            "'Star Wars: Knights of the Old Republic' Script Compiler/Decompiler\n" +
            "based on 'NeverWinter Nights' Script Compiler/Decompiler\n" +
            "Copyright 2002-2003, Edward T. Smith\n" +
            "Modified by Hazard (hazard_x@gmx.net)\n" +
            "Modified further tk102 for stoffe -mkb- (v0.03b)";

        /// <summary>Error strings (addresses in .rdata).</summary>
        public static class ErrorStrings
        {
            public const string UnrecognizedOption = "Error: Unrecognized option \"%c\"\n";
            public const string TooManyArguments = "Error: Too many arguments\n";
            public const string UnableToOpenInputFile = "Error: Unable to open input file \"%s\"\n";
        }

        /// <summary>Where compile behavior is implemented in src.</summary>
        public static string GetCompileImplementationNote()
        {
            return "Compile: KCompiler.ManagedNwnnsscomp.CompileFile / CompileSourceToBytes; "
                   + "CLI: KCompiler.Cli.NwnnsscompCliParser.Parse (supports -c, -o, -g 1|2, --outputdir, --debug, --nwscript).";
        }
    }
}
