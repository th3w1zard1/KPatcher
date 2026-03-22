// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS FileDecompiler.java (decode → analysis → MainPass → NSS). No external compilers.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using KCompiler.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NCSDecomp.Core.Analysis;
using NCSDecomp.Core.Diagnostics;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.ScriptUtils;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core
{
    /// <summary>
    /// Orchestrates managed NCS → NSS decompilation (DeNCS FileDecompiler core path).
    /// </summary>
    public sealed class FileDecompiler
    {
        private readonly ActionsData _actions;
        private readonly ILogger _log;

        public FileDecompiler(ActionsData actions)
            : this(actions, null)
        {
        }

        public FileDecompiler(ActionsData actions, ILogger logger)
        {
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _log = logger ?? NullLogger.Instance;
        }

        /// <summary>Full pipeline: parse tree, analysis, codegen. Requires action table for ACTION opcodes.</summary>
        public string DecompileToNss(byte[] ncsBytes)
        {
            if (ncsBytes == null || ncsBytes.Length == 0)
            {
                throw new ArgumentException("NCS bytes null or empty.", nameof(ncsBytes));
            }

            SubScriptLogger.SetDiagnosticSink(_log);
            try
            {
            string cid = ToolCorrelation.ReadOptional();
            var swTotal = Stopwatch.StartNew();
            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug(
                    "Tool=NCSDecomp.Core Operation=decompile Phase={Phase} CorrelationId={CorrelationId} InputBytes={Bytes}",
                    DecompPhaseNames.DecompDecode,
                    cid ?? "",
                    ncsBytes.Length);
            }

            var swPhase = Stopwatch.StartNew();
            Start ast = NcsParsePipeline.ParseAst(ncsBytes, _actions, _log);
            swPhase.Stop();
            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug(
                    "Tool=NCSDecomp.Core Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs}",
                    DecompPhaseNames.DecompDecode,
                    cid ?? "",
                    swPhase.ElapsedMilliseconds);
            }

            var data = new FileScriptData(_log);
            var nodedata = new NodeAnalysisData();
            var subdata = new SubroutineAnalysisData(nodedata);

            var swPart = Stopwatch.StartNew();
            var setpos = new SetPositions(nodedata);
            ast.Apply(setpos);
            setpos.Done();
            LogDecompPhaseDuration(DecompPhaseNames.DecompAnalysisSetPositions, cid, swPart);

            swPart = Stopwatch.StartNew();
            SetDestinations setdest = null;
            try
            {
                setdest = new SetDestinations(ast, nodedata, subdata);
                ast.Apply(setdest);
            }
            catch (Exception ex)
            {
                setdest?.Done();
                setdest = null;
                _log.LogWarning(
                    ex,
                    "Tool=NCSDecomp.Core Phase={Phase} CorrelationId={CorrelationId} Message=SetDestinations failed; continuing without destination metadata.",
                    DecompPhaseNames.DecompAnalysis,
                    cid ?? "");
            }

            LogDecompPhaseDuration(DecompPhaseNames.DecompAnalysisSetDestinations, cid, swPart);

            swPart = Stopwatch.StartNew();
            try
            {
                var dead = new SetDeadCode(nodedata, subdata, setdest != null ? setdest.GetOrigins() : null);
                ast.Apply(dead);
                dead.Done();
            }
            catch (Exception ex)
            {
                _log.LogWarning(
                    ex,
                    "Tool=NCSDecomp.Core Phase={Phase} CorrelationId={CorrelationId} Message=SetDeadCode failed; continuing without dead-code metadata.",
                    DecompPhaseNames.DecompAnalysis,
                    cid ?? "");
            }

            LogDecompPhaseDuration(DecompPhaseNames.DecompAnalysisSetDeadCode, cid, swPart);

            setdest?.Done();
            setdest = null;

            swPart = Stopwatch.StartNew();
            subdata.SplitOffSubroutines(ast);
            ASubroutine mainsub = subdata.GetMainSub();

            if (mainsub != null)
            {
                var flatten = new FlattenSub(mainsub, nodedata);
                mainsub.Apply(flatten);
                flatten.Done();
                foreach (ASubroutine iterSub in subdata.GetSubroutines())
                {
                    flatten = new FlattenSub(iterSub, nodedata);
                    iterSub.Apply(flatten);
                    flatten.Done();
                }
            }

            LogDecompPhaseDuration(DecompPhaseNames.DecompAnalysisSplitFlatten, cid, swPart);

            swPart = Stopwatch.StartNew();
            DoGlobalVars doglobs = null;
            try
            {
                ASubroutine globSub = subdata.GetGlobalsSub();
                if (globSub != null)
                {
                    doglobs = new DoGlobalVars(nodedata, subdata, _log);
                    globSub.Apply(doglobs);
                    var cleanG = new CleanupPass(doglobs.GetScriptRoot(), nodedata, subdata, doglobs.GetState());
                    cleanG.Apply();
                    subdata.SetGlobalStack(doglobs.GetStack());
                    subdata.GlobalState(doglobs.GetState());
                    cleanG.Done();
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(
                    ex,
                    "Tool=NCSDecomp.Core Phase={Phase} CorrelationId={CorrelationId} Message=DoGlobalVars failed; continuing.",
                    DecompPhaseNames.DecompAnalysis,
                    cid ?? "");
                doglobs?.Done();
                doglobs = null;
            }

            LogDecompPhaseDuration(DecompPhaseNames.DecompAnalysisGlobalVars, cid, swPart);

            swPart = Stopwatch.StartNew();
            var proto = new PrototypeEngine(nodedata, subdata, _actions, FileDecompilerOptions.StrictSignatures, _log);
            proto.Run();
            LogDecompPhaseDuration(DecompPhaseNames.DecompAnalysisPrototypeEngine, cid, swPart);

            swPart = Stopwatch.StartNew();
            if (mainsub != null)
            {
                DoTypes dotypes = new DoTypes(subdata.GetState(mainsub), nodedata, subdata, _actions, false, _log);
                mainsub.Apply(dotypes);
                try
                {
                    dotypes.AssertStack();
                }
                catch (Exception ex)
                {
                    _log.LogWarning(
                        ex,
                        "Tool=NCSDecomp.Core Phase={Phase} CorrelationId={CorrelationId} Message=DoTypes.AssertStack failed for main; continuing (DeNCS parity)",
                        DecompPhaseNames.DecompAnalysis,
                        cid ?? "");
                }

                dotypes.Done();
            }

            bool alldone = false;
            bool onedone = true;
            int donecount = 0;
            try
            {
                alldone = subdata.CountSubsDone() == subdata.NumSubs();
                onedone = true;
                donecount = subdata.CountSubsDone();
            }
            catch (Exception ex)
            {
                _log.LogWarning(
                    ex,
                    "Tool=NCSDecomp.Core Phase={Phase} CorrelationId={CorrelationId} Message=SubroutineCounts query failed; using fallback completion flags",
                    DecompPhaseNames.DecompAnalysis,
                    cid ?? "");
                alldone = true;
                onedone = false;
            }

            for (int loop = 0; !alldone && onedone && loop < 1000; loop++)
            {
                onedone = false;
                foreach (ASubroutine sub in subdata.GetSubroutines())
                {
                    if (sub == null)
                    {
                        continue;
                    }

                    var dt = new DoTypes(subdata.GetState(sub), nodedata, subdata, _actions, false, _log);
                    sub.Apply(dt);
                    dt.Done();
                }

                if (mainsub != null)
                {
                    var dtMain = new DoTypes(subdata.GetState(mainsub), nodedata, subdata, _actions, false, _log);
                    mainsub.Apply(dtMain);
                    dtMain.Done();
                }

                try
                {
                    int newDone = subdata.CountSubsDone();
                    onedone = newDone > donecount;
                    donecount = newDone;
                    alldone = subdata.CountSubsDone() == subdata.NumSubs();
                }
                catch (Exception ex)
                {
                    _log.LogWarning(
                        ex,
                        "Tool=NCSDecomp.Core Phase={Phase} CorrelationId={CorrelationId} Message=DoTypes iteration loop CountSubsDone failed; breaking",
                        DecompPhaseNames.DecompAnalysis,
                        cid ?? "");
                    break;
                }
            }

            LogDecompPhaseDuration(DecompPhaseNames.DecompAnalysisDoTypes, cid, swPart);

            EnforceStrictSignatures(subdata, nodedata, cid);
            nodedata.ClearProtoData();

            swPart = Stopwatch.StartNew();
            foreach (ASubroutine iterSub in subdata.GetSubroutines())
            {
                MainPass mainpass = null;
                CleanupPass cleanpass = null;
                try
                {
                    mainpass = new MainPass(subdata.GetState(iterSub), nodedata, subdata, _actions, _log);
                    iterSub.Apply(mainpass);
                    cleanpass = new CleanupPass(mainpass.GetScriptRoot(), nodedata, subdata, mainpass.GetState());
                    cleanpass.Apply();
                    data.AddSub(mainpass.GetState());
                }
                finally
                {
                    mainpass?.Done();
                    cleanpass?.Done();
                }
            }

            if (mainsub != null)
            {
                MainPass mainpass = null;
                CleanupPass cleanpass = null;
                try
                {
                    mainpass = new MainPass(subdata.GetState(mainsub), nodedata, subdata, _actions, _log);
                    mainsub.Apply(mainpass);
                    try
                    {
                        mainpass.AssertStack();
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning(
                            ex,
                            "Tool=NCSDecomp.Core Phase={Phase} CorrelationId={CorrelationId} Message=MainPass.AssertStack failed for main; continuing",
                            DecompPhaseNames.DecompPrint,
                            cid ?? "");
                    }

                    cleanpass = new CleanupPass(mainpass.GetScriptRoot(), nodedata, subdata, mainpass.GetState());
                    cleanpass.Apply();
                    mainpass.GetState().IsMain(true);
                    data.AddSub(mainpass.GetState());
                }
                finally
                {
                    mainpass?.Done();
                    cleanpass?.Done();
                }
            }

            LogDecompPhaseDuration(DecompPhaseNames.DecompPrintMainPass, cid, swPart);

            data.Subdata(subdata);

            if (doglobs != null)
            {
                CleanupPass cleanpass = null;
                try
                {
                    cleanpass = new CleanupPass(doglobs.GetScriptRoot(), nodedata, subdata, doglobs.GetState());
                    cleanpass.Apply();
                    data.Globals(doglobs.GetState());
                }
                finally
                {
                    doglobs.Done();
                    cleanpass?.Done();
                }
            }

            var destroy = new DestroyParseTree();
            foreach (ASubroutine iterSub in subdata.GetSubroutines())
            {
                iterSub.Apply(destroy);
            }

            if (mainsub != null)
            {
                mainsub.Apply(destroy);
            }

            swPart = Stopwatch.StartNew();
            data.GenerateCode();
            LogDecompPhaseDuration(DecompPhaseNames.DecompPrintAssemble, cid, swPart);

            swTotal.Stop();
            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug(
                    "Tool=NCSDecomp.Core Operation=decompile Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} InputBytes={InputBytes} OutputChars={Chars}",
                    DecompPhaseNames.DecompComplete,
                    cid ?? "",
                    swTotal.ElapsedMilliseconds,
                    ncsBytes.Length,
                    data.GetCode()?.Length ?? 0);
            }

            return data.GetCode();
            }
            finally
            {
                SubScriptLogger.ClearDiagnosticSink();
            }
        }

        private void LogDecompPhaseDuration(string phase, string correlationId, Stopwatch sw)
        {
            if (sw == null)
            {
                return;
            }

            sw.Stop();
            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug(
                    "Tool=NCSDecomp.Core Operation=decompile Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs}",
                    phase,
                    correlationId ?? string.Empty,
                    sw.ElapsedMilliseconds);
            }
        }

        private void EnforceStrictSignatures(SubroutineAnalysisData subdata, NodeAnalysisData nodedata, string correlationId)
        {
            if (!FileDecompilerOptions.StrictSignatures)
            {
                return;
            }

            foreach (ASubroutine iterSub in subdata.GetSubroutines())
            {
                SubroutineState state = subdata.GetState(iterSub);
                if (!state.IsTotallyPrototyped())
                {
                    _log.LogWarning(
                        "Tool=NCSDecomp.Core Phase={Phase} CorrelationId={CorrelationId} Message=Strict signatures: unresolved signature for subroutine at {Pos} (continuing)",
                        DecompPhaseNames.DecompAnalysis,
                        correlationId ?? "",
                        nodedata.GetPos(iterSub));
                }
            }
        }

        /// <summary>Per-script output state (DeNCS FileScriptData).</summary>
        private sealed class FileScriptData
        {
            private readonly ILogger _log;
            private readonly List<SubScriptState> subs = new List<SubScriptState>();
            private SubScriptState globals;
            private SubroutineAnalysisData subdata;
            private string code;

            public FileScriptData(ILogger log)
            {
                _log = log ?? NullLogger.Instance;
            }

            public void AddSub(SubScriptState sub)
            {
                if (sub != null)
                {
                    subs.Add(sub);
                }
            }

            public void Globals(SubScriptState g)
            {
                globals = g;
            }

            public void Subdata(SubroutineAnalysisData sd)
            {
                subdata = sd;
            }

            public string GetCode()
            {
                return code;
            }

            public void GenerateCode()
            {
                string nl = Environment.NewLine;
                if (subs.Count == 0)
                {
                    code = "// No subroutines could be decompiled." + nl + "void main() { }" + nl;
                    return;
                }

                var protobuff = new StringBuilder();
                var fcnbuff = new StringBuilder();
                foreach (SubScriptState state in subs)
                {
                    try
                    {
                        if (!state.IsMain())
                        {
                            string proto = state.GetProto();
                            if (!string.IsNullOrWhiteSpace(proto))
                            {
                                protobuff.Append(proto).Append(";").Append(nl);
                            }
                        }

                        string funcCode = state.toString();
                        if (!string.IsNullOrWhiteSpace(funcCode))
                        {
                            fcnbuff.Append(funcCode).Append(nl);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning(
                            ex,
                            "Tool=NCSDecomp.Core Phase={Phase} Message=subroutine codegen failed; emitting NSS comment only",
                            DecompPhaseNames.DecompPrint);
                        fcnbuff.Append("// Error generating subroutine: ").Append(ex.Message).Append(nl);
                    }
                }

                string globs = "";
                if (globals != null)
                {
                    try
                    {
                        globs = "// Globals" + nl + globals.ToStringGlobals() + nl;
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning(
                            ex,
                            "Tool=NCSDecomp.Core Phase={Phase} Message=globals codegen failed; emitting NSS comment only",
                            DecompPhaseNames.DecompPrint);
                        globs = "// Error: globals — " + ex.Message + nl;
                    }
                }

                string protohdr = "";
                if (protobuff.Length > 0)
                {
                    protohdr = "// Prototypes" + nl;
                }

                string structDecls = "";
                try
                {
                    if (subdata != null)
                    {
                        structDecls = subdata.GetStructDeclarations();
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning(
                        ex,
                        "Tool=NCSDecomp.Core Phase={Phase} Message=GetStructDeclarations failed; continuing without struct block.",
                        DecompPhaseNames.DecompPrint);
                }

                code = structDecls + globs + protohdr + protobuff + fcnbuff.ToString();
            }

            public void Close()
            {
                foreach (SubScriptState s in subs)
                {
                    s.Close();
                }

                subs.Clear();
                if (globals != null)
                {
                    globals.Close();
                    globals = null;
                }

                if (subdata != null)
                {
                    subdata.Close();
                    subdata = null;
                }

                code = null;
            }
        }
    }
}
