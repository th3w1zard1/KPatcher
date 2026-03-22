// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.ScriptUtils;
using NCSDecomp.Core.Stack;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Prototype/type state for one subroutine (DeNCS SubroutineState.java).
    /// </summary>
    public class SubroutineState
    {
        private const byte ProtoNo = 0;
        private const byte ProtoInProgress = 1;
        private const byte ProtoDone = 2;

        private DecompType type;
        private List<DecompType> @params;
        private int returndepth;
        private AstNode root;
        private int paramsize;
        private bool paramstyped;
        private byte status;
        private NodeAnalysisData nodedata;
        private LinkedList<DecisionData> decisionqueue;
        private readonly byte id;

        public SubroutineState(NodeAnalysisData nodedata, AstNode root, byte id)
        {
            this.nodedata = nodedata;
            @params = new List<DecompType>();
            decisionqueue = new LinkedList<DecisionData>();
            paramstyped = true;
            paramsize = 0;
            status = ProtoNo;
            type = new DecompType(DecompType.VtInvalid);
            this.root = root;
            this.id = id;
        }

        public void ParseDone()
        {
            root = null;
            nodedata = null;
            decisionqueue = null;
        }

        public void Close()
        {
            @params = null;
            root = null;
            nodedata = null;
            if (decisionqueue != null)
            {
                foreach (DecisionData d in decisionqueue)
                {
                    d.Close();
                }

                decisionqueue = null;
            }

            type = null;
        }

        public void PrintState()
        {
            SubScriptLogger.Trace("Return type is " + type);
            SubScriptLogger.Trace("There are " + paramsize + " parameters");
            if (paramsize > 0)
            {
                var buff = new System.Text.StringBuilder();
                buff.Append(" Types: ");
                foreach (DecompType pt in @params)
                {
                    buff.Append(pt).Append(" ");
                }

                SubScriptLogger.Trace(buff.ToString());
            }
        }

        public void StartPrototyping()
        {
            status = ProtoInProgress;
        }

        public void StopPrototyping(bool success)
        {
            if (success)
            {
                status = ProtoDone;
                decisionqueue = null;
            }
            else
            {
                status = ProtoNo;
            }
        }

        public bool IsPrototyped()
        {
            return status == ProtoDone;
        }

        public bool IsBeingPrototyped()
        {
            return status == ProtoInProgress;
        }

        public bool IsTotallyPrototyped()
        {
            return status == ProtoDone && @params.Count >= paramsize;
        }

        public bool GetSkipStart(int pos)
        {
            if (decisionqueue != null && decisionqueue.Count > 0)
            {
                DecisionData decision = decisionqueue.First.Value;
                if (nodedata.GetPos(decision.DecisionNode) == pos)
                {
                    if (decision.DoJump())
                    {
                        return true;
                    }

                    decisionqueue.RemoveFirst();
                }

                return false;
            }

            return false;
        }

        public bool GetSkipEnd(int pos)
        {
            if (decisionqueue != null && decisionqueue.Count > 0)
            {
                if (decisionqueue.First.Value.Destination == pos)
                {
                    decisionqueue.RemoveFirst();
                    return true;
                }
            }

            return false;
        }

        public void SetParamCount(int parameters)
        {
            paramsize = parameters;
            if (parameters > 0)
            {
                paramstyped = false;
            }

            EnsureParamPlaceholders();
        }

        public int GetParamCount()
        {
            return paramsize;
        }

        public DecompType Type()
        {
            return type;
        }

        public List<DecompType> Params()
        {
            return @params;
        }

        public void SetReturnType(DecompType t, int depth)
        {
            type = t;
            returndepth = depth;
        }

        public void UpdateParams(LinkedList<DecompType> types)
        {
            paramstyped = true;
            bool redo = @params.Count > 0;
            while (types.Count < paramsize)
            {
                types.AddFirst(new DecompType(DecompType.VtInvalid));
            }

            while (types.Count > paramsize)
            {
                types.RemoveFirst();
            }

            int i = 0;
            foreach (DecompType newtype in types)
            {
                if (redo && !@params[i].IsTyped())
                {
                    @params[i] = newtype;
                }
                else if (!redo)
                {
                    @params.Add(newtype);
                }

                if (!@params[i].IsTyped())
                {
                    paramstyped = false;
                }

                i++;
            }
        }

        public void EnsureParamPlaceholders()
        {
            while (@params.Count < paramsize)
            {
                @params.Add(new DecompType(DecompType.VtInteger));
            }

            while (@params.Count > paramsize)
            {
                @params.RemoveAt(@params.Count - 1);
            }
        }

        public DecompType GetParamType(int pos)
        {
            return @params.Count < pos ? new DecompType(DecompType.VtNone) : @params[pos - 1];
        }

        public void InitStack(LocalTypeStack stack)
        {
            if (!IsPrototyped())
            {
                return;
            }

            if (type.IsTyped() && !type.Equals(DecompType.VtNone))
            {
                StructType st = type as StructType;
                if (st == null)
                {
                    stack.Push(type);
                }
                else
                {
                    foreach (DecompType structtype in st.Types())
                    {
                        stack.Push(structtype);
                    }
                }
            }

            if (paramsize == @params.Count)
            {
                for (int i = 0; i < paramsize; i++)
                {
                    stack.Push(@params[i]);
                }
            }
            else
            {
                for (int i = 0; i < paramsize; i++)
                {
                    stack.Push(new DecompType(DecompType.VtInvalid));
                }
            }
        }

        public void InitStack(LocalVarStack stack)
        {
            if (!type.Equals(DecompType.VtNone))
            {
                Variable retvar;
                if (type is StructType)
                {
                    retvar = new VarStruct((StructType)type);
                }
                else
                {
                    retvar = new Variable(type);
                }

                retvar.IsReturn(true);
                stack.Push(retvar);
            }

            for (int i = 0; i < paramsize; i++)
            {
                Variable paramvar = new Variable(@params[i]);
                paramvar.IsParam(true);
                stack.Push(paramvar);
            }
        }

        public byte GetId()
        {
            return id;
        }

        public bool AreParamsTyped()
        {
            return paramstyped;
        }

        public int GetStart()
        {
            return nodedata.GetPos(root);
        }

        public int GetEnd()
        {
            return NodeUtils.GetSubEnd((ASubroutine)root);
        }

        public void AddDecision(AstNode node, int destination)
        {
            decisionqueue.AddLast(new DecisionData(node, destination, false));
            if (decisionqueue.Count > 3000)
            {
                throw new InvalidOperationException("Decision queue size over 3000 - probable infinite loop");
            }
        }

        public void AddJump(AstNode node, int destination)
        {
            decisionqueue.AddLast(new DecisionData(node, destination, true));
        }

        public int GetCurrentDestination()
        {
            DecisionData data = decisionqueue.Last.Value;
            if (data == null)
            {
                throw new InvalidOperationException("Attempted to get a destination but no decision nodes found.");
            }

            return data.Destination;
        }

        public int SwitchDecision()
        {
            while (decisionqueue.Count > 0)
            {
                DecisionData data = decisionqueue.Last.Value;
                if (data.SwitchDecision())
                {
                    return data.Destination;
                }

                decisionqueue.RemoveLast();
            }

            return -1;
        }

        private class DecisionData
        {
            public AstNode DecisionNode;
            public byte Decision;
            public int Destination;

            public DecisionData(AstNode node, int destination, bool forcejump)
            {
                Decision = forcejump ? (byte)2 : (byte)1;
                DecisionNode = node;
                Destination = destination;
            }

            public bool DoJump()
            {
                return Decision != 1;
            }

            public bool SwitchDecision()
            {
                if (Decision == 1)
                {
                    Decision = 0;
                    return true;
                }

                return false;
            }

            public void Close()
            {
                DecisionNode = null;
            }
        }
    }
}
