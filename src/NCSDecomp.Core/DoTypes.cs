// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS DoTypes.java.

using System;
using NCSDecomp.Core.Analysis;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.Stack;
using NCSDecomp.Core.Utils;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core
{
    /// <summary>
    /// Stack-type inference pass for subroutine prototyping (DeNCS DoTypes.java).
    /// </summary>
    public sealed class DoTypes : PrunedDepthFirstAdapter
    {
        private SubroutineState state;
        private LocalTypeStack stack = new LocalTypeStack();
        private NodeAnalysisData nodedata;
        private SubroutineAnalysisData subdata;
        private ActionsData actions;
        private readonly bool initialproto;
        private bool protoskipping;
        private readonly bool protoreturn;
        private bool skipdeadcode;
        private LocalTypeStack backupstack;

        public DoTypes(SubroutineState state, NodeAnalysisData nodedata, SubroutineAnalysisData subdata, ActionsData actions, bool initialprototyping)
        {
            this.nodedata = nodedata;
            this.subdata = subdata;
            this.state = state;
            this.actions = actions;
            if (!initialprototyping)
            {
                this.state.InitStack(stack);
            }

            initialproto = initialprototyping;
            protoskipping = false;
            skipdeadcode = false;
            protoreturn = initialproto || !state.Type().IsTyped();
        }

        public void Done()
        {
            state = null;
            if (stack != null)
            {
                stack.Close();
                stack = null;
            }

            nodedata = null;
            subdata = null;
            if (backupstack != null)
            {
                backupstack.Close();
                backupstack = null;
            }

            actions = null;
        }

        public void AssertStack()
        {
            if (stack.Size() > 0)
            {
                Console.WriteLine("Uh-oh... dumping main() state:");
                state.PrintState();
                throw new InvalidOperationException("Error: Final stack size " + stack.Size());
            }
        }

        public override void OutARsaddCommand(ARsaddCommand node)
        {
            if (!protoskipping && !skipdeadcode)
            {
                stack.Push(NodeUtils.GetType(node));
            }
        }

        public override void OutACopyDownSpCommand(ACopyDownSpCommand node)
        {
            if (!protoskipping && !skipdeadcode)
            {
                int copy = NodeUtils.StackSizeToPos(node.GetSize());
                int loc = NodeUtils.StackOffsetToPos(node.GetOffset());
                bool isstruct = copy > 1;
                if (protoreturn && loc > stack.Size())
                {
                    if (isstruct)
                    {
                        StructType st = new StructType();
                        for (int i = copy; i >= 1; i--)
                        {
                            st.AddType(stack.Get(i));
                        }

                        state.SetReturnType(st, loc - stack.Size());
                        subdata.AddStruct(st);
                    }
                    else
                    {
                        state.SetReturnType(stack.Get(1, state), loc - stack.Size());
                    }
                }
            }
        }

        public override void OutACopyTopSpCommand(ACopyTopSpCommand node)
        {
            if (!protoskipping && !skipdeadcode)
            {
                int copy = NodeUtils.StackSizeToPos(node.GetSize());
                int loc = NodeUtils.StackOffsetToPos(node.GetOffset());
                for (int i = 0; i < copy; i++)
                {
                    stack.Push(stack.Get(loc, state));
                }
            }
        }

        public override void OutAConstCommand(AConstCommand node)
        {
            if (!protoskipping && !skipdeadcode)
            {
                stack.Push(NodeUtils.GetType(node));
            }
        }

        public override void OutAActionCommand(AActionCommand node)
        {
            if (!protoskipping && !skipdeadcode)
            {
                int remove = NodeUtils.ActionRemoveElementCount(node, actions);
                DecompType rettype;
                try
                {
                    rettype = NodeUtils.GetReturnType(node, actions);
                }
                catch (InvalidOperationException)
                {
                    rettype = new DecompType(DecompType.VtNone);
                }

                int add = NodeUtils.StackSizeToPos(rettype.TypeSize());
                stack.Remove(remove);
                for (int i = 0; i < add; i++)
                {
                    stack.Push(rettype);
                }
            }
        }

        public override void OutALogiiCommand(ALogiiCommand node)
        {
            if (!protoskipping && !skipdeadcode)
            {
                stack.Remove(2);
                stack.Push(new DecompType(DecompType.VtInteger));
            }
        }

        public override void OutABinaryCommand(ABinaryCommand node)
        {
            if (!protoskipping && !skipdeadcode)
            {
                int sizep1;
                int sizep2;
                int sizeresult;
                DecompType resulttype;
                if (NodeUtils.IsEqualityOp(node))
                {
                    if (NodeUtils.GetType(node).Equals(DecompType.VtStructstruct))
                    {
                        sizep1 = NodeUtils.StackSizeToPos(node.GetSize());
                        sizep2 = sizep1;
                    }
                    else
                    {
                        sizep2 = 1;
                        sizep1 = 1;
                    }

                    sizeresult = 1;
                    resulttype = new DecompType(DecompType.VtInteger);
                }
                else if (NodeUtils.IsVectorAllowedOp(node))
                {
                    sizep1 = NodeUtils.GetParam1Size(node);
                    sizep2 = NodeUtils.GetParam2Size(node);
                    sizeresult = NodeUtils.GetResultSize(node);
                    resulttype = NodeUtils.GetReturnType(node);
                }
                else
                {
                    sizep1 = 1;
                    sizep2 = 1;
                    sizeresult = 1;
                    resulttype = new DecompType(DecompType.VtInteger);
                }

                stack.Remove(sizep1 + sizep2);
                for (int i = 0; i < sizeresult; i++)
                {
                    stack.Push(resulttype);
                }
            }
        }

        public override void OutAMoveSpCommand(AMoveSpCommand node)
        {
            if (!protoskipping && !skipdeadcode)
            {
                if (initialproto)
                {
                    int remove = NodeUtils.StackOffsetToPos(node.GetOffset());
                    int @params = stack.RemovePrototyping(remove);
                    if (@params > 8)
                    {
                        @params = 8;
                    }

                    if (@params > 0)
                    {
                        int current = state.GetParamCount();
                        if (current == 0 || @params < current)
                        {
                            state.SetParamCount(@params);
                        }
                    }
                }
                else
                {
                    stack.Remove(NodeUtils.StackOffsetToPos(node.GetOffset()));
                }
            }
        }

        public override void OutAStoreStateCommand(AStoreStateCommand node)
        {
        }

        public override void OutAConditionalJumpCommand(AConditionalJumpCommand node)
        {
            if (!protoskipping && !skipdeadcode)
            {
                stack.Remove(1);
            }

            CheckProtoskippingStart(node);
            if (!protoskipping && !skipdeadcode && !IsLogOr(node))
            {
                StoreStackState(nodedata.GetDestination(node));
            }
        }

        public override void OutAJumpCommand(AJumpCommand node)
        {
            CheckProtoskippingStart(node);
            if (!protoskipping && !skipdeadcode)
            {
                StoreStackState(nodedata.GetDestination(node));
            }
        }

        public override void OutAJumpToSubroutine(AJumpToSubroutine node)
        {
            if (!protoskipping && !skipdeadcode)
            {
                AstNode destNode = nodedata.GetDestination(node);
                SubroutineState substate = subdata.GetState(destNode);
                if (!substate.IsPrototyped())
                {
                    throw new InvalidOperationException("Hit JSR on unprototyped subroutine " + nodedata.GetPos(destNode));
                }

                int paramsize = substate.GetParamCount();
                if (substate.IsTotallyPrototyped())
                {
                    stack.Remove(paramsize);
                    if (protoreturn && substate.Type().Equals(DecompType.VtInvalid))
                    {
                        substate.SetReturnType(stack.Get(1, state), 0);
                    }
                }
                else
                {
                    stack.RemoveParams(paramsize, substate);
                    if (substate.Type().Equals(DecompType.VtInvalid))
                    {
                        substate.SetReturnType(stack.Get(1, state), 0);
                    }

                    if (substate.Type().Equals(DecompType.VtStruct) && !substate.Type().IsTyped())
                    {
                        StructType st = (StructType)substate.Type();
                        int sz = substate.Type().Size();
                        for (int i = 0; i < sz; i++)
                        {
                            DecompType t = stack.Get(sz - i, state);
                            if (!t.Equals(DecompType.VtInvalid))
                            {
                                st.UpdateType(i, t);
                            }
                        }
                    }
                }
            }
        }

        public override void OutADestructCommand(ADestructCommand node)
        {
            if (!protoskipping && !skipdeadcode)
            {
                int removesize = NodeUtils.StackSizeToPos(node.GetSizeRem());
                int savestart = NodeUtils.StackSizeToPos(node.GetOffset());
                int savesize = NodeUtils.StackSizeToPos(node.GetSizeSave());
                stack.Remove(removesize - (savesize + savestart));
                stack.Remove(savesize + 1, savestart);
            }
        }

        public override void OutACopyTopBpCommand(ACopyTopBpCommand node)
        {
            if (!protoskipping && !skipdeadcode)
            {
                int copy = NodeUtils.StackSizeToPos(node.GetSize());
                int loc = NodeUtils.StackOffsetToPos(node.GetOffset());
                for (int i = 0; i < copy; i++)
                {
                    stack.Push(subdata.GetGlobalStack().GetType(loc));
                    loc--;
                }
            }
        }

        public override void OutACopyDownBpCommand(ACopyDownBpCommand node)
        {
        }

        public override void OutAReturn(AReturn node)
        {
            if (!protoskipping && !skipdeadcode && protoreturn)
            {
                DecompType rtype = NodeUtils.GetType(node);
                if (rtype != null && rtype.IsTyped())
                {
                    if (!state.Type().IsTyped() || state.Type().Equals(DecompType.VtInvalid))
                    {
                        state.SetReturnType(rtype, 0);
                    }
                }
            }
        }

        public override void OutASubroutine(ASubroutine node)
        {
            if (initialproto)
            {
                state.StopPrototyping(true);
            }
        }

        public override void DefaultIn(AstNode node)
        {
            if (!protoskipping)
            {
                RestoreStackState(node);
            }
            else
            {
                CheckProtoskippingDone(node);
            }

            if (NodeUtils.IsCommandNode(node))
            {
                skipdeadcode = nodedata.DeadCode(node);
            }
        }

        private void CheckProtoskippingDone(AstNode node)
        {
            if (state.GetSkipEnd(nodedata.GetPos(node)))
            {
                protoskipping = false;
            }
        }

        private void CheckProtoskippingStart(AstNode node)
        {
            if (state.GetSkipStart(nodedata.GetPos(node)))
            {
                protoskipping = true;
            }
        }

        private void StoreStackState(AstNode node)
        {
            if (NodeUtils.IsStoreStackNode(node))
            {
                nodedata.SetStack(node, stack.CloneStack(), true);
            }
        }

        private void RestoreStackState(AstNode node)
        {
            LocalTypeStack restore = nodedata.GetStack(node) as LocalTypeStack;
            if (restore != null)
            {
                stack = restore;
            }
        }

        private bool IsLogOr(AstNode node)
        {
            return nodedata.LogOrCode(node);
        }
    }
}
