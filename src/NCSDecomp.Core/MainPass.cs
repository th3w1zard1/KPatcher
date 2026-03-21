// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.ScriptNode;
using NCSDecomp.Core.ScriptUtils;
using NCSDecomp.Core.Stack;
using NCSDecomp.Core.Utils;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core
{
    /// <summary>
    /// Converts the annotated parse tree into script text (DeNCS MainPass.java).
    /// </summary>
    public class MainPass : PrunedDepthFirstAdapter
    {
        protected LocalVarStack stack = new LocalVarStack();
        protected NodeAnalysisData nodedata;
        protected SubroutineAnalysisData subdata;
        protected bool skipdeadcode;
        protected SubScriptState state;
        private ActionsData actions;
        protected bool globals;
        protected LocalVarStack backupstack;
        protected DecompType type;

        public MainPass(SubroutineState protostate, NodeAnalysisData nodedata, SubroutineAnalysisData subdata, ActionsData actions)
        {
            this.nodedata = nodedata;
            this.subdata = subdata;
            this.actions = actions;
            protostate.InitStack(stack);
            skipdeadcode = false;
            state = new SubScriptState(nodedata, subdata, stack, protostate, actions, FileDecompilerOptions.PreferSwitches);
            globals = false;
            backupstack = null;
            type = protostate.Type();
        }

        protected MainPass(NodeAnalysisData nodedata, SubroutineAnalysisData subdata)
        {
            this.nodedata = nodedata;
            this.subdata = subdata;
            skipdeadcode = false;
            state = new SubScriptState(nodedata, subdata, stack, FileDecompilerOptions.PreferSwitches);
            globals = true;
            backupstack = null;
            type = new DecompType(DecompType.VtInvalid);
        }

        public void Done()
        {
            stack = null;
            nodedata = null;
            subdata = null;
            if (state != null)
            {
                state.ParseDone();
            }

            state = null;
            actions = null;
            backupstack = null;
            type = null;
        }

        public void AssertStack()
        {
            if ((type.Equals((byte)0) || type.Equals(unchecked((byte)-1))) && stack.Size() > 0)
            {
                throw new InvalidOperationException("Error: Final stack size " + stack.Size() + stack);
            }
        }

        public string GetCode()
        {
            return state.toString();
        }

        public string GetProto()
        {
            return state.GetProto();
        }

        public ASub GetScriptRoot()
        {
            return state.GetRoot();
        }

        public SubScriptState GetState()
        {
            return state;
        }

        public override void OutARsaddCommand(ARsaddCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    var var = new Variable(NodeUtils.GetType(node));
                    stack.Push(var);
                    state.TransformRSAdd(node);
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutACopyDownSpCommand(ACopyDownSpCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    int copy = NodeUtils.StackSizeToPos(node.GetSize());
                    int loc = NodeUtils.StackOffsetToPos(node.GetOffset());
                    if (copy > 1)
                    {
                        stack.Structify(loc - copy + 1, copy, subdata);
                    }

                    state.TransformCopyDownSp(node);
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutACopyTopSpCommand(ACopyTopSpCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    VarStruct varstruct = null;
                    int copy = NodeUtils.StackSizeToPos(node.GetSize());
                    int loc = NodeUtils.StackOffsetToPos(node.GetOffset());
                    if (copy > 1)
                    {
                        varstruct = stack.Structify(loc - copy + 1, copy, subdata);
                    }

                    state.TransformCopyTopSp(node);
                    if (copy > 1)
                    {
                        stack.Push(varstruct);
                    }
                    else
                    {
                        for (int i = 0; i < copy; i++)
                        {
                            StackEntry entry = stack.Get(loc);
                            stack.Push(entry);
                        }
                    }
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutAConstCommand(AConstCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    DecompType t = NodeUtils.GetType(node);
                    Const aconst;
                    switch (t.ByteValue())
                    {
                        case DecompType.VtInteger:
                            aconst = Const.NewConst(t, NodeUtils.GetIntConstValue(node));
                            break;
                        case DecompType.VtFloat:
                            aconst = Const.NewConst(t, NodeUtils.GetFloatConstValue(node));
                            break;
                        case DecompType.VtString:
                            aconst = Const.NewConst(t, NodeUtils.GetStringConstValue(node));
                            break;
                        case DecompType.VtObject:
                            aconst = Const.NewConst(t, NodeUtils.GetObjectConstValue(node));
                            break;
                        default:
                            throw new InvalidOperationException("Invalid const type " + t);
                    }

                    stack.Push(aconst);
                    state.TransformConst(node);
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutAActionCommand(AActionCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    int remove = NodeUtils.ActionRemoveElementCount(node, actions);
                    int i = 0;
                    while (i < remove)
                    {
                        StackEntry entry = RemoveFromStack();
                        i += entry.Size();
                    }

                    DecompType retType;
                    try
                    {
                        retType = NodeUtils.GetReturnType(node, actions);
                    }
                    catch (Exception)
                    {
                        retType = new DecompType((byte)0);
                    }

                    if (!retType.Equals(unchecked((byte)-16)))
                    {
                        if (!retType.Equals((byte)0))
                        {
                            var var = new Variable(retType);
                            stack.Push(var);
                        }
                    }
                    else
                    {
                        for (int ix = 0; ix < 3; ix++)
                        {
                            var var = new Variable((byte)4);
                            stack.Push(var);
                        }

                        stack.Structify(1, 3, subdata);
                    }

                    state.TransformAction(node);
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutALogiiCommand(ALogiiCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    RemoveFromStack();
                    RemoveFromStack();
                    var var = new Variable((byte)3);
                    stack.Push(var);
                    state.TransformLogii(node);
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutABinaryCommand(ABinaryCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    int sizep1;
                    int sizep2;
                    int sizeresult;
                    DecompType resulttype;
                    if (NodeUtils.IsEqualityOp(node))
                    {
                        if (NodeUtils.GetType(node).Equals((byte)36))
                        {
                            sizep1 = sizep2 = NodeUtils.StackSizeToPos(node.GetSize());
                        }
                        else
                        {
                            sizep2 = 1;
                            sizep1 = 1;
                        }

                        sizeresult = 1;
                        resulttype = new DecompType((byte)3);
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
                        resulttype = new DecompType((byte)3);
                    }

                    for (int i = 0; i < sizep1 + sizep2; i++)
                    {
                        RemoveFromStack();
                    }

                    for (int i = 0; i < sizeresult; i++)
                    {
                        var var = new Variable(resulttype);
                        stack.Push(var);
                    }

                    state.TransformBinary(node);
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutAUnaryCommand(AUnaryCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () => state.TransformUnary(node));
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutAMoveSpCommand(AMoveSpCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    state.TransformMoveSp(node);
                    backupstack = stack.CloneStack();
                    int remove = NodeUtils.StackOffsetToPos(node.GetOffset());
                    var entries = new List<Variable>();
                    int i = 0;
                    while (i < remove)
                    {
                        StackEntry entry = RemoveFromStack();
                        i += entry.Size();
                        var v = entry as Variable;
                        if (v != null && !v.IsPlaceholder(stack) && !v.IsOnStack(stack))
                        {
                            entries.Add(v);
                        }
                    }

                    if (entries.Count > 0 && !nodedata.DeadCode(node))
                    {
                        state.TransformMoveSPVariablesRemoved(entries, node);
                    }
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutAConditionalJumpCommand(AConditionalJumpCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    if (nodedata.LogOrCode(node))
                    {
                        state.TransformLogOrExtraJump(node);
                    }
                    else
                    {
                        state.TransformConditionalJump(node);
                    }

                    RemoveFromStack();
                    if (!nodedata.LogOrCode(node))
                    {
                        StoreStackState(nodedata.GetDestination(node), nodedata.DeadCode(node));
                    }
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutAJumpCommand(AJumpCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    state.TransformJump(node);
                    StoreStackState(nodedata.GetDestination(node), nodedata.DeadCode(node));
                    if (backupstack != null)
                    {
                        stack.DoneWithStack();
                        stack = backupstack;
                        state.SetStack(stack);
                    }
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutAJumpToSubroutine(AJumpToSubroutine node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    SubroutineState substate = subdata.GetState(nodedata.GetDestination(node));
                    int paramsize = substate.GetParamCount();
                    for (int i = 0; i < paramsize; i++)
                    {
                        RemoveFromStack();
                    }

                    state.TransformJSR(node);
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutADestructCommand(ADestructCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    state.TransformDestruct(node);
                    int removesize = NodeUtils.StackSizeToPos(node.GetSizeRem());
                    int savestart = NodeUtils.StackSizeToPos(node.GetOffset());
                    int savesize = NodeUtils.StackSizeToPos(node.GetSizeSave());
                    stack.Destruct(removesize, savestart, savesize, subdata);
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutACopyTopBpCommand(ACopyTopBpCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    VarStruct varstruct = null;
                    int copy = NodeUtils.StackSizeToPos(node.GetSize());
                    int loc = NodeUtils.StackOffsetToPos(node.GetOffset());
                    if (copy > 1)
                    {
                        varstruct = subdata.GetGlobalStack().Structify(loc - copy + 1, copy, subdata);
                    }

                    state.TransformCopyTopBp(node);
                    if (copy > 1)
                    {
                        stack.Push(varstruct);
                    }
                    else
                    {
                        for (int i = 0; i < copy; i++)
                        {
                            var v = (Variable)subdata.GetGlobalStack().Get(loc);
                            stack.Push(v);
                            loc--;
                        }
                    }
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutACopyDownBpCommand(ACopyDownBpCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    int copy = NodeUtils.StackSizeToPos(node.GetSize());
                    int loc = NodeUtils.StackOffsetToPos(node.GetOffset());
                    if (copy > 1)
                    {
                        subdata.GetGlobalStack().Structify(loc - copy + 1, copy, subdata);
                    }

                    state.TransformCopyDownBp(node);
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutAStoreStateCommand(AStoreStateCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    state.TransformStoreState(node);
                    backupstack = null;
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutAStackCommand(AStackCommand node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () => state.TransformStack(node));
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutAReturn(AReturn node)
        {
            if (!skipdeadcode)
            {
                WithRecovery(node, () => state.TransformReturn(node));
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutASubroutine(ASubroutine node)
        {
        }

        public override void OutAProgram(AProgram node)
        {
        }

        public override void DefaultIn(AstNode node)
        {
            RestoreStackState(node);
            CheckOrigins(node);
            if (NodeUtils.IsCommandNode(node))
            {
                skipdeadcode = !nodedata.ProcessCode(node);
            }
        }

        private StackEntry RemoveFromStack()
        {
            StackEntry entry = stack.Remove();
            var v = entry as Variable;
            if (v != null && v.IsPlaceholder(stack))
            {
                state.TransformPlaceholderVariableRemoved(v);
            }

            return entry;
        }

        private void StoreStackState(AstNode destNode, bool isdead)
        {
            if (NodeUtils.IsStoreStackNode(destNode))
            {
                nodedata.SetStack(destNode, stack.CloneStack(), false);
            }
        }

        private void RestoreStackState(AstNode node)
        {
            LocalVarStack restore = nodedata.GetStack(node) as LocalVarStack;
            if (restore != null)
            {
                stack.DoneWithStack();
                stack = restore;
                state.SetStack(stack);
                if (backupstack != null)
                {
                    backupstack.DoneWithStack();
                }

                backupstack = null;
            }
        }

        protected void WithRecovery(AstNode node, Action action)
        {
            LocalVarStack stackSnapshot = stack.CloneStack();
            LocalVarStack backupSnapshot = backupstack != null ? backupstack.CloneStack() : null;
            try
            {
                action();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Decompiler recovery at position " + nodedata.GetPos(node) + ": " + e.Message);
                stack = stackSnapshot;
                state.SetStack(stack);
                backupstack = backupSnapshot;
                state.EmitError(node, nodedata.GetPos(node));
            }
        }

        private void CheckOrigins(AstNode node)
        {
            AstNode origin;
            while ((origin = nodedata.RemoveLastOrigin(node)) != null)
            {
                state.TransformOriginFound(node, origin);
            }
        }
    }
}
