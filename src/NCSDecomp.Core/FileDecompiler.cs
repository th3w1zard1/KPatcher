// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS FileDecompiler.java (decode → analysis → MainPass → NSS). No external compilers.

using System;
using System.Collections.Generic;
using System.Text;
using NCSDecomp.Core.Analysis;
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

        public FileDecompiler(ActionsData actions)
        {
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        }

        /// <summary>Full pipeline: parse tree, analysis, codegen. Requires action table for ACTION opcodes.</summary>
        public string DecompileToNss(byte[] ncsBytes)
        {
            if (ncsBytes == null || ncsBytes.Length == 0)
            {
                throw new ArgumentException("NCS bytes null or empty.", nameof(ncsBytes));
            }

            Start ast = NcsParsePipeline.ParseAst(ncsBytes, _actions);
            var data = new FileScriptData();
            var nodedata = new NodeAnalysisData();
            var subdata = new SubroutineAnalysisData(nodedata);

            var setpos = new SetPositions(nodedata);
            ast.Apply(setpos);
            setpos.Done();

            SetDestinations setdest = null;
            try
            {
                setdest = new SetDestinations(ast, nodedata, subdata);
                ast.Apply(setdest);
            }
            catch (Exception)
            {
                setdest?.Done();
                setdest = null;
            }

            try
            {
                var dead = new SetDeadCode(nodedata, subdata, setdest != null ? setdest.GetOrigins() : null);
                ast.Apply(dead);
                dead.Done();
            }
            catch (Exception)
            {
                // continue without dead-code metadata
            }

            setdest?.Done();
            setdest = null;

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

            DoGlobalVars doglobs = null;
            try
            {
                ASubroutine globSub = subdata.GetGlobalsSub();
                if (globSub != null)
                {
                    doglobs = new DoGlobalVars(nodedata, subdata);
                    globSub.Apply(doglobs);
                    var cleanG = new CleanupPass(doglobs.GetScriptRoot(), nodedata, subdata, doglobs.GetState());
                    cleanG.Apply();
                    subdata.SetGlobalStack(doglobs.GetStack());
                    subdata.GlobalState(doglobs.GetState());
                    cleanG.Done();
                }
            }
            catch (Exception)
            {
                doglobs?.Done();
                doglobs = null;
            }

            var proto = new PrototypeEngine(nodedata, subdata, _actions, FileDecompilerOptions.StrictSignatures);
            proto.Run();

            if (mainsub != null)
            {
                DoTypes dotypes = new DoTypes(subdata.GetState(mainsub), nodedata, subdata, _actions, false);
                mainsub.Apply(dotypes);
                try
                {
                    dotypes.AssertStack();
                }
                catch (Exception)
                {
                    // DeNCS continues
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
            catch (Exception)
            {
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

                    var dt = new DoTypes(subdata.GetState(sub), nodedata, subdata, _actions, false);
                    sub.Apply(dt);
                    dt.Done();
                }

                if (mainsub != null)
                {
                    var dtMain = new DoTypes(subdata.GetState(mainsub), nodedata, subdata, _actions, false);
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
                catch (Exception)
                {
                    break;
                }
            }

            EnforceStrictSignatures(subdata, nodedata);
            nodedata.ClearProtoData();

            foreach (ASubroutine iterSub in subdata.GetSubroutines())
            {
                MainPass mainpass = null;
                CleanupPass cleanpass = null;
                try
                {
                    mainpass = new MainPass(subdata.GetState(iterSub), nodedata, subdata, _actions);
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
                    mainpass = new MainPass(subdata.GetState(mainsub), nodedata, subdata, _actions);
                    mainsub.Apply(mainpass);
                    try
                    {
                        mainpass.AssertStack();
                    }
                    catch (Exception)
                    {
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

            data.GenerateCode();
            return data.GetCode();
        }

        private static void EnforceStrictSignatures(SubroutineAnalysisData subdata, NodeAnalysisData nodedata)
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
                    Console.WriteLine("Strict signatures: unresolved signature for subroutine at "
                        + nodedata.GetPos(iterSub) + " (continuing)");
                }
            }
        }

        /// <summary>Per-script output state (DeNCS FileScriptData).</summary>
        private sealed class FileScriptData
        {
            private readonly List<SubScriptState> subs = new List<SubScriptState>();
            private SubScriptState globals;
            private SubroutineAnalysisData subdata;
            private string code;

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
                catch (Exception)
                {
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
