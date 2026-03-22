namespace KCompiler.Diagnostics
{
    public static class CompilePhaseNames
    {
        /// <summary>nwnnsscomp-style argv parsing (before compile).</summary>
        public const string CliParse = "cli.parse";

        public const string IoReadNss = "io.read_nss";
        public const string CompileParse = "compile.parse";
        public const string CompileCodegen = "compile.codegen";
        public const string Optimize = "optimize";
        public const string IoWriteNcs = "io.write_ncs";
    }
}
