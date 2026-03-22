// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.ScriptUtils;
using NCSDecomp.Core.Stack;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Cross-subroutine analysis container (DeNCS SubroutineAnalysisData.java).
    /// </summary>
    public class SubroutineAnalysisData
    {
        private NodeAnalysisData nodedata;
        private Dictionary<int, ASubroutine> subroutines;
        private Hashtable substates;
        private ASubroutine mainsub;
        private ASubroutine globalsub;
        private LocalVarStack globalstack;
        private List<StructType> globalstructs;
        private SubScriptState globalstate;

        public SubroutineAnalysisData(NodeAnalysisData nodedata)
        {
            this.nodedata = nodedata;
            subroutines = new Dictionary<int, ASubroutine>();
            substates = new Hashtable();
            globalsub = null;
            globalstack = null;
            mainsub = null;
            globalstructs = new List<StructType>();
        }

        public void ParseDone()
        {
            nodedata = null;
            if (substates != null)
            {
                foreach (SubroutineState s in substates.Values)
                {
                    s.ParseDone();
                }

                substates = null;
            }

            subroutines = null;
            mainsub = null;
            globalsub = null;
            globalstate = null;
        }

        public void Close()
        {
            if (nodedata != null)
            {
                nodedata.Close();
                nodedata = null;
            }

            if (substates != null)
            {
                foreach (SubroutineState s in substates.Values)
                {
                    s.Close();
                }

                substates = null;
            }

            if (subroutines != null)
            {
                subroutines.Clear();
                subroutines = null;
            }

            mainsub = null;
            globalsub = null;
            if (globalstack != null)
            {
                globalstack.Close();
                globalstack = null;
            }

            if (globalstructs != null)
            {
                foreach (StructType st in globalstructs)
                {
                    st.Close();
                }

                globalstructs = null;
            }

            if (globalstate != null)
            {
                globalstate.Close();
                globalstate = null;
            }
        }

        public void PrintStates()
        {
            foreach (AstNode subnode in substates.Keys)
            {
                SubroutineState state = (SubroutineState)substates[subnode];
                SubScriptLogger.Trace("Printing state for subroutine at " + nodedata.GetPos(subnode));
                state.PrintState();
            }
        }

        public SubScriptState GlobalState()
        {
            return globalstate;
        }

        public void GlobalState(SubScriptState gs)
        {
            globalstate = gs;
        }

        public ASubroutine GetGlobalsSub()
        {
            return globalsub;
        }

        public void SetGlobalsSub(ASubroutine globalsub)
        {
            this.globalsub = globalsub;
        }

        public ASubroutine GetMainSub()
        {
            return mainsub;
        }

        public void SetMainSub(ASubroutine mainsub)
        {
            this.mainsub = mainsub;
        }

        public LocalVarStack GetGlobalStack()
        {
            return globalstack;
        }

        public void SetGlobalStack(LocalVarStack stack)
        {
            globalstack = stack;
        }

        public int NumSubs()
        {
            return subroutines.Count;
        }

        public int CountSubsDone()
        {
            int count = 0;
            foreach (SubroutineState s in substates.Values)
            {
                if (s.IsTotallyPrototyped())
                {
                    count++;
                }
            }

            return count;
        }

        public SubroutineState GetState(AstNode sub)
        {
            return (SubroutineState)substates[sub];
        }

        public bool IsPrototyped(int pos, bool nullok)
        {
            if (!subroutines.TryGetValue(pos, out ASubroutine sub))
            {
                if (nullok)
                {
                    return false;
                }

                throw new InvalidOperationException("Checking prototype on a subroutine not in the hash");
            }

            SubroutineState state = (SubroutineState)substates[sub];
            return state != null && state.IsPrototyped();
        }

        public bool IsBeingPrototyped(int pos)
        {
            if (!subroutines.TryGetValue(pos, out ASubroutine sub))
            {
                throw new InvalidOperationException("Checking prototype on a subroutine not in the hash");
            }

            SubroutineState state = (SubroutineState)substates[sub];
            return state != null && state.IsBeingPrototyped();
        }

        public bool IsFullyPrototyped(int pos)
        {
            if (!subroutines.TryGetValue(pos, out ASubroutine sub))
            {
                throw new InvalidOperationException("Checking prototype on a subroutine not in the hash");
            }

            SubroutineState state = (SubroutineState)substates[sub];
            return state != null && state.IsTotallyPrototyped();
        }

        public void AddStruct(StructType st)
        {
            if (!globalstructs.Contains(st))
            {
                globalstructs.Add(st);
                st.TypeName("structtype" + globalstructs.Count);
            }
        }

        public void AddStruct(VarStruct vs)
        {
            StructType structtype = vs.StructType();
            if (!globalstructs.Contains(structtype))
            {
                globalstructs.Add(structtype);
                structtype.TypeName("structtype" + globalstructs.Count);
            }
            else
            {
                vs.StructType(GetStructPrototype(structtype));
            }
        }

        public string GetStructDeclarations()
        {
            var buff = new StringBuilder();
            for (int i = 0; i < globalstructs.Count; i++)
            {
                StructType structtype = globalstructs[i];
                if (!structtype.IsVector())
                {
                    buff.Append(structtype.ToDeclString()).Append(" {\r\n");
                    List<DecompType> types = structtype.Types();
                    for (int j = 0; j < types.Count; j++)
                    {
                        buff.Append("\t").Append(types[j].ToDeclString()).Append(" ").Append(structtype.ElementName(j)).Append(";\r\n");
                    }

                    buff.Append("};\r\n\r\n");
                }
            }

            return buff.ToString();
        }

        public string GetStructTypeName(StructType structtype)
        {
            StructType protostruct = GetStructPrototype(structtype);
            return protostruct.TypeName();
        }

        public StructType GetStructPrototype(StructType structtype)
        {
            int index = globalstructs.IndexOf(structtype);
            if (index == -1)
            {
                globalstructs.Add(structtype);
                index = globalstructs.Count - 1;
            }

            return globalstructs[index];
        }

        private void AddSubroutine(int pos, ASubroutine node, byte id)
        {
            subroutines[pos] = node;
            AddSubState(node, id);
        }

        private void AddSubState(AstNode sub, byte id)
        {
            SubroutineState state = new SubroutineState(nodedata, sub, id);
            substates[sub] = state;
        }

        private void AddMain(ASubroutine sub, bool conditional)
        {
            mainsub = sub;
            if (conditional)
            {
                SubroutineState state = new SubroutineState(nodedata, mainsub, 0);
                state.SetReturnType(new DecompType(DecompType.VtInteger), 0);
                substates[mainsub] = state;
            }
            else
            {
                AddSubState(mainsub, 0);
            }
        }

        private void AddGlobals(ASubroutine sub)
        {
            globalsub = sub;
        }

        public IEnumerable<ASubroutine> GetSubroutines()
        {
            return subroutines.Values;
        }

        public void SplitOffSubroutines(Start ast)
        {
            bool conditional = NodeUtils.IsConditionalProgram(ast);
            TypedLinkedList<PSubroutine> subs = ((AProgram)ast.GetPProgram()).GetSubroutine();
            var list = subs.ToList();
            ASubroutine node = (ASubroutine)list[0];
            list.RemoveAt(0);
            if (list.Count > 0 && IsGlobalsSub(node))
            {
                AddGlobals(node);
                node = (ASubroutine)list[0];
                list.RemoveAt(0);
            }

            AddMain(node, conditional);
            byte id = 1;
            while (list.Count > 0)
            {
                node = (ASubroutine)list[0];
                list.RemoveAt(0);
                AddSubroutine(nodedata.GetPos(node), node, id++);
            }
        }

        private bool IsGlobalsSub(ASubroutine node)
        {
            var cig = new CheckIsGlobals();
            node.Apply(cig);
            return cig.GetIsGlobals();
        }
    }
}
