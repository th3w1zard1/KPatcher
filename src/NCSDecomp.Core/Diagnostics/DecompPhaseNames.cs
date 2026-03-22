namespace NCSDecomp.Core.Diagnostics
{
    public static class DecompPhaseNames
    {
        public const string CliParse = "cli.parse";
        public const string IoReadNcs = "io.read_ncs";
        public const string DecompDecode = "decomp.decode";
        /// <summary>Decoder only: NCS bytes → DeNCS token string (before SableCC).</summary>
        public const string DecompBytecodeToTokens = "decomp.bytecode_to_tokens";
        /// <summary>SableCC lexer + parser only.</summary>
        public const string DecompSableCc = "decomp.sablecc";
        public const string DecompAnalysis = "decomp.analysis";
        public const string DecompAnalysisSetPositions = "decomp.analysis.set_positions";
        public const string DecompAnalysisSetDestinations = "decomp.analysis.set_destinations";
        public const string DecompAnalysisSetDeadCode = "decomp.analysis.set_dead_code";
        public const string DecompAnalysisSplitFlatten = "decomp.analysis.split_flatten";
        public const string DecompAnalysisGlobalVars = "decomp.analysis.global_vars";
        public const string DecompAnalysisPrototypeEngine = "decomp.analysis.prototype_engine";
        public const string DecompAnalysisDoTypes = "decomp.analysis.do_types";
        public const string DecompPrintMainPass = "decomp.print.main_pass";
        public const string DecompPrintAssemble = "decomp.print.assemble_nss";
        public const string ActionsLoad = "actions.load";
        public const string ConfigLoad = "config.load";
        public const string ConfigSave = "config.save";
        public const string RoundTripActions = "round_trip.actions";
        public const string RoundTripCompile = "round_trip.compile";
        public const string RoundTripDecode = "round_trip.decode_compare";
        public const string RoundTripGetSiblingNcs = "round_trip.get_sibling_ncs";
        public const string UiOpenNcs = "ui.open_ncs";
        public const string UiDecompile = "ui.decompile";
        public const string UiRoundTrip = "ui.round_trip";

        public const string DecompPrint = "decomp.print";
        /// <summary>Full decode → analysis → print pipeline finished (NSS text ready).</summary>
        public const string DecompComplete = "decomp.complete";

        public const string IoWriteNss = "io.write_nss";
    }
}
