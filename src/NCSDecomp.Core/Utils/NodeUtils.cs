// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS NodeUtils.java (subset and helpers; extend as passes land).

using System;
using System.Collections.Generic;
using NCSDecomp.Core;
using NCSDecomp.Core.Node;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Utils
{
    public static class NodeUtils
    {
        public const int CmdsizeJump = 6;
        public const int CmdsizeRetn = 2;

        public static bool IsConditionalProgram(Start ast)
        {
            return ((AProgram)ast.GetPProgram()).GetConditional() != null;
        }

        public static int GetSubEnd(ASubroutine sub)
        {
            AstNode ret = sub.GetReturn() as AstNode;
            AReturnCmd wrap = ret as AReturnCmd;
            if (wrap != null)
            {
                ret = wrap.GetReturn() as AstNode;
            }

            return GetCommandPos(ret);
        }

        public static int GetCommandPos(AstNode node)
        {
            AConditionalJumpCommand cj = node as AConditionalJumpCommand;
            if (cj != null)
            {
                return int.Parse(cj.GetPos().GetText());
            }

            AJumpCommand jc = node as AJumpCommand;
            if (jc != null)
            {
                return int.Parse(jc.GetPos().GetText());
            }

            AJumpToSubroutine js = node as AJumpToSubroutine;
            if (js != null)
            {
                return int.Parse(js.GetPos().GetText());
            }

            AReturn ret = node as AReturn;
            if (ret != null)
            {
                return int.Parse(ret.GetPos().GetText());
            }

            ACopyDownSpCommand cds = node as ACopyDownSpCommand;
            if (cds != null)
            {
                return int.Parse(cds.GetPos().GetText());
            }

            ACopyTopSpCommand cts = node as ACopyTopSpCommand;
            if (cts != null)
            {
                return int.Parse(cts.GetPos().GetText());
            }

            ACopyDownBpCommand cdb = node as ACopyDownBpCommand;
            if (cdb != null)
            {
                return int.Parse(cdb.GetPos().GetText());
            }

            ACopyTopBpCommand ctb = node as ACopyTopBpCommand;
            if (ctb != null)
            {
                return int.Parse(ctb.GetPos().GetText());
            }

            AMoveSpCommand mv = node as AMoveSpCommand;
            if (mv != null)
            {
                return int.Parse(mv.GetPos().GetText());
            }

            ARsaddCommand rs = node as ARsaddCommand;
            if (rs != null)
            {
                return int.Parse(rs.GetPos().GetText());
            }

            AConstCommand cc = node as AConstCommand;
            if (cc != null)
            {
                return int.Parse(cc.GetPos().GetText());
            }

            AActionCommand ac = node as AActionCommand;
            if (ac != null)
            {
                return int.Parse(ac.GetPos().GetText());
            }

            ALogiiCommand lc = node as ALogiiCommand;
            if (lc != null)
            {
                return int.Parse(lc.GetPos().GetText());
            }

            ABinaryCommand bc = node as ABinaryCommand;
            if (bc != null)
            {
                return int.Parse(bc.GetPos().GetText());
            }

            AUnaryCommand uc = node as AUnaryCommand;
            if (uc != null)
            {
                return int.Parse(uc.GetPos().GetText());
            }

            AStackCommand sc = node as AStackCommand;
            if (sc != null)
            {
                return int.Parse(sc.GetPos().GetText());
            }

            ADestructCommand dc = node as ADestructCommand;
            if (dc != null)
            {
                return int.Parse(dc.GetPos().GetText());
            }

            ABpCommand bp = node as ABpCommand;
            if (bp != null)
            {
                return int.Parse(bp.GetPos().GetText());
            }

            AStoreStateCommand st = node as AStoreStateCommand;
            if (st != null)
            {
                return int.Parse(st.GetPos().GetText());
            }

            return -1;
        }

        private static TIntegerConstant CmdTypeOfCommand(AstNode node)
        {
            AConditionalJumpCommand cj = node as AConditionalJumpCommand;
            if (cj != null)
            {
                return cj.GetCmdType();
            }

            AJumpCommand jc = node as AJumpCommand;
            if (jc != null)
            {
                return jc.GetCmdType();
            }

            AJumpToSubroutine js = node as AJumpToSubroutine;
            if (js != null)
            {
                return js.GetCmdType();
            }

            AReturn ret = node as AReturn;
            if (ret != null)
            {
                return ret.GetCmdType();
            }

            ACopyDownSpCommand cds = node as ACopyDownSpCommand;
            if (cds != null)
            {
                return cds.GetCmdType();
            }

            ACopyTopSpCommand cts = node as ACopyTopSpCommand;
            if (cts != null)
            {
                return cts.GetCmdType();
            }

            ACopyDownBpCommand cdb = node as ACopyDownBpCommand;
            if (cdb != null)
            {
                return cdb.GetCmdType();
            }

            ACopyTopBpCommand ctb = node as ACopyTopBpCommand;
            if (ctb != null)
            {
                return ctb.GetCmdType();
            }

            AMoveSpCommand mv = node as AMoveSpCommand;
            if (mv != null)
            {
                return mv.GetCmdType();
            }

            ARsaddCommand rs = node as ARsaddCommand;
            if (rs != null)
            {
                return rs.GetCmdType();
            }

            AConstCommand cc = node as AConstCommand;
            if (cc != null)
            {
                return cc.GetCmdType();
            }

            AActionCommand ac = node as AActionCommand;
            if (ac != null)
            {
                return ac.GetCmdType();
            }

            ALogiiCommand lc = node as ALogiiCommand;
            if (lc != null)
            {
                return lc.GetCmdType();
            }

            ABinaryCommand bc = node as ABinaryCommand;
            if (bc != null)
            {
                return bc.GetCmdType();
            }

            AUnaryCommand uc = node as AUnaryCommand;
            if (uc != null)
            {
                return uc.GetCmdType();
            }

            AStackCommand sc = node as AStackCommand;
            if (sc != null)
            {
                return sc.GetCmdType();
            }

            ADestructCommand dc = node as ADestructCommand;
            if (dc != null)
            {
                return dc.GetCmdType();
            }

            ABpCommand bp = node as ABpCommand;
            if (bp != null)
            {
                return bp.GetCmdType();
            }

            return null;
        }

        public static int GetJumpDestinationPos(AstNode node)
        {
            AConditionalJumpCommand cj = node as AConditionalJumpCommand;
            if (cj != null)
            {
                return int.Parse(cj.GetPos().GetText()) + int.Parse(cj.GetOffset().GetText());
            }

            AJumpCommand jc = node as AJumpCommand;
            if (jc != null)
            {
                return int.Parse(jc.GetPos().GetText()) + int.Parse(jc.GetOffset().GetText());
            }

            AJumpToSubroutine js = node as AJumpToSubroutine;
            if (js != null)
            {
                return int.Parse(js.GetPos().GetText()) + int.Parse(js.GetOffset().GetText());
            }

            return -1;
        }

        public static bool IsCommandNode(AstNode node)
        {
            return node is AConditionalJumpCommand
                || node is AJumpCommand
                || node is AJumpToSubroutine
                || node is AReturn
                || node is ACopyDownSpCommand
                || node is ACopyTopSpCommand
                || node is ACopyDownBpCommand
                || node is ACopyTopBpCommand
                || node is AMoveSpCommand
                || node is ARsaddCommand
                || node is AConstCommand
                || node is AActionCommand
                || node is ALogiiCommand
                || node is ABinaryCommand
                || node is AUnaryCommand
                || node is AStackCommand
                || node is ADestructCommand
                || node is ABpCommand
                || node is AStoreStateCommand;
        }

        public static int StackOffsetToPos(TIntegerConstant offset)
        {
            return -int.Parse(offset.GetText()) / 4;
        }

        public static int StackSizeToPos(TIntegerConstant offset)
        {
            return int.Parse(offset.GetText()) / 4;
        }

        public static int StackSizeToPos(int offset)
        {
            return offset / 4;
        }

        public static DecompType GetType(AstNode node)
        {
            TIntegerConstant tc = CmdTypeOfCommand(node);
            if (tc == null)
            {
                throw new InvalidOperationException("No command type for this node: " + node);
            }

            return new DecompType(unchecked((byte)int.Parse(tc.GetText())));
        }

        public static DecompType GetReturnType(AActionCommand node, ActionsData actions)
        {
            return actions.GetReturnType(GetActionId(node));
        }

        public static int GetActionId(AActionCommand node)
        {
            return int.Parse(node.GetId().GetText());
        }

        public static int GetActionParamCount(AActionCommand node)
        {
            return int.Parse(node.GetArgCount().GetText());
        }

        public static List<DecompType> GetActionParamTypes(AActionCommand node, ActionsData actions)
        {
            if (actions == null)
            {
                throw new InvalidOperationException("ActionsData is null when trying to get param types for action ID: " + GetActionId(node));
            }

            return actions.GetParamTypes(GetActionId(node));
        }

        public static int ActionRemoveElementCount(AActionCommand node, ActionsData actions)
        {
            try
            {
                List<DecompType> types = GetActionParamTypes(node, actions);
                int count = Math.Min(GetActionParamCount(node), types.Count);
                int remove = 0;
                for (int i = 0; i < count; i++)
                {
                    remove += types[i].TypeSize();
                }

                return StackSizeToPos(remove);
            }
            catch (InvalidOperationException)
            {
                int argBytes = GetActionParamCount(node);
                return StackSizeToPos(argBytes);
            }
        }

        public static long GetIntConstValue(AConstCommand node)
        {
            PConstant pconst = node.GetConstant();
            DecompType type = GetType(node);
            if (type.ByteValue() != DecompType.VtInteger)
            {
                throw new InvalidOperationException("Expected int const type (3), got " + type);
            }

            return long.Parse(((AIntConstant)pconst).GetIntegerConstant().GetText());
        }

        public static float GetFloatConstValue(AConstCommand node)
        {
            PConstant pconst = node.GetConstant();
            DecompType type = GetType(node);
            if (type.ByteValue() != DecompType.VtFloat)
            {
                throw new InvalidOperationException("Expected float const type (4), got " + type);
            }

            AIntConstant asInt = pconst as AIntConstant;
            if (asInt != null)
            {
                long intValue = long.Parse(asInt.GetIntegerConstant().GetText());
                return intValue;
            }
            AFloatConstant asFloat = pconst as AFloatConstant;
            if (asFloat != null)
            {
                return float.Parse(asFloat.GetFloatConstant().GetText(), System.Globalization.CultureInfo.InvariantCulture);
            }

            throw new InvalidOperationException("Expected AFloatConstant or AIntConstant, got " + pconst.GetType().Name);
        }

        public static string GetStringConstValue(AConstCommand node)
        {
            PConstant pconst = node.GetConstant();
            DecompType type = GetType(node);
            if (type.ByteValue() != DecompType.VtString)
            {
                throw new InvalidOperationException("Expected string const type (5), got " + type);
            }

            return ((AStringConstant)pconst).GetStringLiteral().GetText();
        }

        public static int GetObjectConstValue(AConstCommand node)
        {
            PConstant pconst = node.GetConstant();
            DecompType type = GetType(node);
            if (type.ByteValue() != DecompType.VtObject)
            {
                throw new InvalidOperationException("Expected object const type (6), got " + type);
            }

            return int.Parse(((AIntConstant)pconst).GetIntegerConstant().GetText());
        }

        public static bool IsEqualityOp(ABinaryCommand node)
        {
            PBinaryOp op = node.GetBinaryOp();
            return op is AEqualBinaryOp || op is ANequalBinaryOp;
        }

        public static bool IsVectorAllowedOp(ABinaryCommand node)
        {
            PBinaryOp op = node.GetBinaryOp();
            return op is AAddBinaryOp || op is ASubBinaryOp || op is ADivBinaryOp || op is AMulBinaryOp;
        }

        public static int GetParam1Size(ABinaryCommand node)
        {
            byte tb = GetType(node).ByteValue();
            return tb != DecompType.VtVectorfloat && tb != DecompType.VtVectorvector ? 1 : 3;
        }

        public static int GetParam2Size(ABinaryCommand node)
        {
            byte tb = GetType(node).ByteValue();
            return tb != DecompType.VtFloatvector && tb != DecompType.VtVectorvector ? 1 : 3;
        }

        public static int GetResultSize(ABinaryCommand node)
        {
            byte tb = GetType(node).ByteValue();
            return tb != DecompType.VtFloatvector && tb != DecompType.VtVectorfloat && tb != DecompType.VtVectorvector ? 1 : 3;
        }

        public static DecompType GetReturnType(ABinaryCommand node)
        {
            byte nodetype = unchecked((byte)int.Parse(node.GetCmdType().GetText()));
            byte ty;
            if (nodetype == 60 || nodetype == 59 || nodetype == 58)
            {
                ty = unchecked((byte)-16);
            }
            else if (nodetype == 32)
            {
                ty = 3;
            }
            else if (nodetype != 37 && nodetype != 38 && nodetype != 33)
            {
                if (nodetype != 35)
                {
                    throw new InvalidOperationException("Unexpected type " + nodetype);
                }

                ty = 5;
            }
            else
            {
                ty = 4;
            }

            if (ty == unchecked((byte)-16))
            {
                ty = 4;
            }

            return new DecompType(ty);
        }

        public static bool IsStoreStackNode(AstNode node)
        {
            ALogiiCmd lc = node as ALogiiCmd;
            if (lc != null)
            {
                ALogiiCommand lnode = lc.GetLogiiCommand() as ALogiiCommand;
                if (lnode != null && lnode.GetLogiiOp() is AOrLogiiOp)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Unwraps a <see cref="PCmd"/> wrapper (or subroutine root) to the inner bytecode command node.
        /// </summary>
        public static AstNode GetCommandChild(AstNode node)
        {
            if (IsCommandNode(node))
            {
                return node;
            }

            ASubroutine sub = node as ASubroutine;
            if (sub != null)
            {
                return GetCommandChild(sub.GetCommandBlock());
            }

            ACommandBlock block = node as ACommandBlock;
            if (block != null)
            {
                PCmd first = block.GetCmd().FirstOrDefault();
                return first != null ? GetCommandChild(first) : null;
            }

            AAddVarCmd av = node as AAddVarCmd;
            if (av != null)
            {
                return GetCommandChild(av.GetRsaddCommand());
            }

            AActionJumpCmd aj = node as AActionJumpCmd;
            if (aj != null)
            {
                return GetCommandChild(aj.GetStoreStateCommand());
            }

            AConstCmd cc = node as AConstCmd;
            if (cc != null)
            {
                return GetCommandChild(cc.GetConstCommand());
            }

            ACopydownspCmd cds = node as ACopydownspCmd;
            if (cds != null)
            {
                return GetCommandChild(cds.GetCopyDownSpCommand());
            }

            ACopytopspCmd cts = node as ACopytopspCmd;
            if (cts != null)
            {
                return GetCommandChild(cts.GetCopyTopSpCommand());
            }

            ACopydownbpCmd cdb = node as ACopydownbpCmd;
            if (cdb != null)
            {
                return GetCommandChild(cdb.GetCopyDownBpCommand());
            }

            ACopytopbpCmd ctb = node as ACopytopbpCmd;
            if (ctb != null)
            {
                return GetCommandChild(ctb.GetCopyTopBpCommand());
            }

            ACondJumpCmd cj = node as ACondJumpCmd;
            if (cj != null)
            {
                return GetCommandChild(cj.GetConditionalJumpCommand());
            }

            AJumpCmd jc = node as AJumpCmd;
            if (jc != null)
            {
                return GetCommandChild(jc.GetJumpCommand());
            }

            AJumpSubCmd js = node as AJumpSubCmd;
            if (js != null)
            {
                return GetCommandChild(js.GetJumpToSubroutine());
            }

            AMovespCmd mv = node as AMovespCmd;
            if (mv != null)
            {
                return GetCommandChild(mv.GetMoveSpCommand());
            }

            ALogiiCmd lc = node as ALogiiCmd;
            if (lc != null)
            {
                return GetCommandChild(lc.GetLogiiCommand());
            }

            AUnaryCmd uc = node as AUnaryCmd;
            if (uc != null)
            {
                return GetCommandChild(uc.GetUnaryCommand());
            }

            ABinaryCmd bc = node as ABinaryCmd;
            if (bc != null)
            {
                return GetCommandChild(bc.GetBinaryCommand());
            }

            ADestructCmd dc = node as ADestructCmd;
            if (dc != null)
            {
                return GetCommandChild(dc.GetDestructCommand());
            }

            ABpCmd bp = node as ABpCmd;
            if (bp != null)
            {
                return GetCommandChild(bp.GetBpCommand());
            }

            AActionCmd ac = node as AActionCmd;
            if (ac != null)
            {
                return GetCommandChild(ac.GetActionCommand());
            }

            AStackOpCmd sc = node as AStackOpCmd;
            if (sc != null)
            {
                return GetCommandChild(sc.GetStackCommand());
            }

            AReturnCmd rc = node as AReturnCmd;
            if (rc != null)
            {
                return GetCommandChild(rc.GetReturn());
            }

            AStoreStateCmd st = node as AStoreStateCmd;
            if (st != null)
            {
                return GetCommandChild(st.GetStoreStateCommand());
            }

            throw new InvalidOperationException("unexpected node type " + (node != null ? node.GetType().FullName : "null"));
        }

        /// <summary>
        /// Next command in the same <see cref="ACommandBlock"/> as <paramref name="node"/>, or null.
        /// </summary>
        public static AstNode GetNextCommand(AstNode node, NodeAnalysisData nodedata)
        {
            AstNode up = node.Parent();
            while (up != null && !(up is ACommandBlock))
            {
                up = up.Parent();
            }

            if (up == null)
            {
                return null;
            }

            int searchPos = nodedata.GetPos(node);
            List<PCmd> cmds = ((ACommandBlock)up).GetCmd().ToList();
            for (int i = 0; i < cmds.Count; i++)
            {
                if (nodedata.GetPos(cmds[i]) == searchPos)
                {
                    if (i + 1 < cmds.Count)
                    {
                        return GetCommandChild(cmds[i + 1]);
                    }

                    return null;
                }
            }

            return null;
        }

        public static AstNode GetPreviousCommand(AstNode node, NodeAnalysisData nodedata)
        {
            if (node is AReturn)
            {
                AstNode p = node.Parent();
                ASubroutine sub = p as ASubroutine;
                if (sub == null)
                {
                    return null;
                }

                ACommandBlock ablock = sub.GetCommandBlock() as ACommandBlock;
                if (ablock == null)
                {
                    return null;
                }

                PCmd last = ablock.GetCmd().GetLast();
                return last != null ? GetCommandChild(last) : null;
            }

            AstNode up = node.Parent();
            while (up != null && !(up is ACommandBlock))
            {
                up = up.Parent();
            }

            if (up == null)
            {
                return null;
            }

            int searchPos = nodedata.GetPos(node);
            List<PCmd> cmds = ((ACommandBlock)up).GetCmd().ToList();
            for (int i = 0; i < cmds.Count; i++)
            {
                if (nodedata.GetPos(cmds[i]) == searchPos)
                {
                    if (i > 0)
                    {
                        return GetCommandChild(cmds[i - 1]);
                    }

                    return null;
                }
            }

            return null;
        }

        public static bool IsJzPastOne(AstNode node)
        {
            AConditionalJumpCommand cj = node as AConditionalJumpCommand;
            if (cj == null)
            {
                return false;
            }

            return cj.GetJumpIf() is AZeroJumpIf
                && int.Parse(cj.GetOffset().GetText()) == 12;
        }

        public static bool IsJz(AstNode node)
        {
            AConditionalJumpCommand cj = node as AConditionalJumpCommand;
            return cj != null && cj.GetJumpIf() is AZeroJumpIf;
        }

        public static bool IsConditionalOp(ABinaryCommand node)
        {
            PBinaryOp op = node.GetBinaryOp();
            return op is AEqualBinaryOp || op is ANequalBinaryOp || op is ALtBinaryOp || op is ALeqBinaryOp
                || op is AGtBinaryOp || op is AGeqBinaryOp;
        }

        public static bool IsArithmeticOp(ABinaryCommand node)
        {
            PBinaryOp op = node.GetBinaryOp();
            return op is AAddBinaryOp || op is ASubBinaryOp || op is ADivBinaryOp || op is AMulBinaryOp
                || op is AModBinaryOp || op is AShleftBinaryOp || op is AShrightBinaryOp || op is AUnrightBinaryOp;
        }

        public static string GetOp(AUnaryCommand node)
        {
            PUnaryOp op = node.GetUnaryOp();
            if (op is ANegUnaryOp)
            {
                return "-";
            }

            if (op is ACompUnaryOp)
            {
                return "~";
            }

            if (op is ANotUnaryOp)
            {
                return "!";
            }

            throw new InvalidOperationException("unknown unary op");
        }

        public static string GetOp(ABinaryCommand node)
        {
            PBinaryOp op = node.GetBinaryOp();
            if (op is AAddBinaryOp)
            {
                return "+";
            }

            if (op is ASubBinaryOp)
            {
                return "-";
            }

            if (op is ADivBinaryOp)
            {
                return "/";
            }

            if (op is AMulBinaryOp)
            {
                return "*";
            }

            if (op is AModBinaryOp)
            {
                return "%";
            }

            if (op is AShleftBinaryOp)
            {
                return "<<";
            }

            if (op is AShrightBinaryOp)
            {
                return ">>";
            }

            if (op is AUnrightBinaryOp)
            {
                throw new InvalidOperationException("found an unsigned bit shift.");
            }

            if (op is AEqualBinaryOp)
            {
                return "==";
            }

            if (op is ANequalBinaryOp)
            {
                return "!=";
            }

            if (op is ALtBinaryOp)
            {
                return "<";
            }

            if (op is ALeqBinaryOp)
            {
                return "<=";
            }

            if (op is AGtBinaryOp)
            {
                return ">";
            }

            if (op is AGeqBinaryOp)
            {
                return ">=";
            }

            throw new InvalidOperationException("unknown binary op");
        }

        public static string GetOp(ALogiiCommand node)
        {
            PLogiiOp op = node.GetLogiiOp();
            if (op is AAndLogiiOp)
            {
                return "&&";
            }

            if (op is AOrLogiiOp)
            {
                return "||";
            }

            if (op is AInclOrLogiiOp)
            {
                return "|";
            }

            if (op is AExclOrLogiiOp)
            {
                return "^";
            }

            if (op is ABitAndLogiiOp)
            {
                return "&";
            }

            throw new InvalidOperationException("unknown logii op");
        }

        public static string GetOp(AStackCommand node)
        {
            PStackOp op = node.GetStackOp();
            if (op is ADecispStackOp || op is ADecibpStackOp)
            {
                return "--";
            }

            if (op is AIncispStackOp || op is AIncibpStackOp)
            {
                return "++";
            }

            throw new InvalidOperationException("unknown relative-to-stack unary modifier op");
        }

        public static bool IsGlobalStackOp(AStackCommand node)
        {
            PStackOp op = node.GetStackOp();
            return op is AIncibpStackOp || op is ADecibpStackOp;
        }

        public static string GetActionName(AActionCommand node, ActionsData actions)
        {
            return actions.GetName(GetActionId(node));
        }

        public static bool IsReturn(AstNode node)
        {
            return node is AReturnCmd || node is AReturn;
        }
    }
}
