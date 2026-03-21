// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;
using NCSDecomp.Core.Node;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Analysis
{
    public class PrunedDepthFirstAdapter : AnalysisAdapter
    {
        public virtual void InStart(Start node)
        {
            DefaultIn(node);
        }

        public virtual void OutStart(Start node)
        {
            DefaultOut(node);
        }

        public virtual void DefaultIn(AstNode node)
        {
        }

        public virtual void DefaultOut(AstNode node)
        {
        }

        public override void CaseStart(Start node)
        {
            InStart(node);
            node.GetPProgram().Apply(this);
            OutStart(node);
        }

        public virtual void InAProgram(AProgram node)
        {
            DefaultIn(node);
        }

        public virtual void OutAProgram(AProgram node)
        {
            DefaultOut(node);
        }

        public override void CaseAProgram(AProgram node)
        {
            InAProgram(node);
            if (node.GetSize() != null)
            {
                node.GetSize().Apply(this);
            }

            if (node.GetConditional() != null)
            {
                node.GetConditional().Apply(this);
            }

            if (node.GetJumpToSubroutine() != null)
            {
                node.GetJumpToSubroutine().Apply(this);
            }

            if (node.GetReturn() != null)
            {
                node.GetReturn().Apply(this);
            }

            var temp = node.GetSubroutine().ToList();

            for (int i = 0; i < temp.Count; i++)
            {
                temp[i].Apply(this);
            }

            OutAProgram(node);
        }

        public virtual void InASubroutine(ASubroutine node)
        {
            DefaultIn(node);
        }

        public virtual void OutASubroutine(ASubroutine node)
        {
            DefaultOut(node);
        }

        public override void CaseASubroutine(ASubroutine node)
        {
            InASubroutine(node);
            if (node.GetCommandBlock() != null)
            {
                node.GetCommandBlock().Apply(this);
            }

            if (node.GetReturn() != null)
            {
                node.GetReturn().Apply(this);
            }

            OutASubroutine(node);
        }

        public virtual void InACommandBlock(ACommandBlock node)
        {
            DefaultIn(node);
        }

        public virtual void OutACommandBlock(ACommandBlock node)
        {
            DefaultOut(node);
        }

        public override void CaseACommandBlock(ACommandBlock node)
        {
            InACommandBlock(node);
            var temp = node.GetCmd().ToList();

            for (int i = 0; i < temp.Count; i++)
            {
                temp[i].Apply(this);
            }

            OutACommandBlock(node);
        }

        public virtual void InAAddVarCmd(AAddVarCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutAAddVarCmd(AAddVarCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseAAddVarCmd(AAddVarCmd node)
        {
            InAAddVarCmd(node);
            if (node.GetRsaddCommand() != null)
            {
                node.GetRsaddCommand().Apply(this);
            }

            OutAAddVarCmd(node);
        }

        public virtual void InAActionJumpCmd(AActionJumpCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutAActionJumpCmd(AActionJumpCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseAActionJumpCmd(AActionJumpCmd node)
        {
            InAActionJumpCmd(node);
            if (node.GetStoreStateCommand() != null)
            {
                node.GetStoreStateCommand().Apply(this);
            }

            if (node.GetJumpCommand() != null)
            {
                node.GetJumpCommand().Apply(this);
            }

            if (node.GetCommandBlock() != null)
            {
                node.GetCommandBlock().Apply(this);
            }

            if (node.GetReturn() != null)
            {
                node.GetReturn().Apply(this);
            }

            OutAActionJumpCmd(node);
        }

        public virtual void InAConstCmd(AConstCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutAConstCmd(AConstCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseAConstCmd(AConstCmd node)
        {
            InAConstCmd(node);
            if (node.GetConstCommand() != null)
            {
                node.GetConstCommand().Apply(this);
            }

            OutAConstCmd(node);
        }

        public virtual void InACopydownspCmd(ACopydownspCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutACopydownspCmd(ACopydownspCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseACopydownspCmd(ACopydownspCmd node)
        {
            InACopydownspCmd(node);
            if (node.GetCopyDownSpCommand() != null)
            {
                node.GetCopyDownSpCommand().Apply(this);
            }

            OutACopydownspCmd(node);
        }

        public virtual void InACopytopspCmd(ACopytopspCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutACopytopspCmd(ACopytopspCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseACopytopspCmd(ACopytopspCmd node)
        {
            InACopytopspCmd(node);
            if (node.GetCopyTopSpCommand() != null)
            {
                node.GetCopyTopSpCommand().Apply(this);
            }

            OutACopytopspCmd(node);
        }

        public virtual void InACopydownbpCmd(ACopydownbpCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutACopydownbpCmd(ACopydownbpCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseACopydownbpCmd(ACopydownbpCmd node)
        {
            InACopydownbpCmd(node);
            if (node.GetCopyDownBpCommand() != null)
            {
                node.GetCopyDownBpCommand().Apply(this);
            }

            OutACopydownbpCmd(node);
        }

        public virtual void InACopytopbpCmd(ACopytopbpCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutACopytopbpCmd(ACopytopbpCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseACopytopbpCmd(ACopytopbpCmd node)
        {
            InACopytopbpCmd(node);
            if (node.GetCopyTopBpCommand() != null)
            {
                node.GetCopyTopBpCommand().Apply(this);
            }

            OutACopytopbpCmd(node);
        }

        public virtual void InACondJumpCmd(ACondJumpCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutACondJumpCmd(ACondJumpCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseACondJumpCmd(ACondJumpCmd node)
        {
            InACondJumpCmd(node);
            if (node.GetConditionalJumpCommand() != null)
            {
                node.GetConditionalJumpCommand().Apply(this);
            }

            OutACondJumpCmd(node);
        }

        public virtual void InAJumpCmd(AJumpCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutAJumpCmd(AJumpCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseAJumpCmd(AJumpCmd node)
        {
            InAJumpCmd(node);
            if (node.GetJumpCommand() != null)
            {
                node.GetJumpCommand().Apply(this);
            }

            OutAJumpCmd(node);
        }

        public virtual void InAJumpSubCmd(AJumpSubCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutAJumpSubCmd(AJumpSubCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseAJumpSubCmd(AJumpSubCmd node)
        {
            InAJumpSubCmd(node);
            if (node.GetJumpToSubroutine() != null)
            {
                node.GetJumpToSubroutine().Apply(this);
            }

            OutAJumpSubCmd(node);
        }

        public virtual void InAMovespCmd(AMovespCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutAMovespCmd(AMovespCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseAMovespCmd(AMovespCmd node)
        {
            InAMovespCmd(node);
            if (node.GetMoveSpCommand() != null)
            {
                node.GetMoveSpCommand().Apply(this);
            }

            OutAMovespCmd(node);
        }

        public virtual void InALogiiCmd(ALogiiCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutALogiiCmd(ALogiiCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseALogiiCmd(ALogiiCmd node)
        {
            InALogiiCmd(node);
            if (node.GetLogiiCommand() != null)
            {
                node.GetLogiiCommand().Apply(this);
            }

            OutALogiiCmd(node);
        }

        public virtual void InAUnaryCmd(AUnaryCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutAUnaryCmd(AUnaryCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseAUnaryCmd(AUnaryCmd node)
        {
            InAUnaryCmd(node);
            if (node.GetUnaryCommand() != null)
            {
                node.GetUnaryCommand().Apply(this);
            }

            OutAUnaryCmd(node);
        }

        public virtual void InABinaryCmd(ABinaryCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutABinaryCmd(ABinaryCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseABinaryCmd(ABinaryCmd node)
        {
            InABinaryCmd(node);
            if (node.GetBinaryCommand() != null)
            {
                node.GetBinaryCommand().Apply(this);
            }

            OutABinaryCmd(node);
        }

        public virtual void InADestructCmd(ADestructCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutADestructCmd(ADestructCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseADestructCmd(ADestructCmd node)
        {
            InADestructCmd(node);
            if (node.GetDestructCommand() != null)
            {
                node.GetDestructCommand().Apply(this);
            }

            OutADestructCmd(node);
        }

        public virtual void InABpCmd(ABpCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutABpCmd(ABpCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseABpCmd(ABpCmd node)
        {
            InABpCmd(node);
            if (node.GetBpCommand() != null)
            {
                node.GetBpCommand().Apply(this);
            }

            OutABpCmd(node);
        }

        public virtual void InAActionCmd(AActionCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutAActionCmd(AActionCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseAActionCmd(AActionCmd node)
        {
            InAActionCmd(node);
            if (node.GetActionCommand() != null)
            {
                node.GetActionCommand().Apply(this);
            }

            OutAActionCmd(node);
        }

        public virtual void InAStackOpCmd(AStackOpCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutAStackOpCmd(AStackOpCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseAStackOpCmd(AStackOpCmd node)
        {
            InAStackOpCmd(node);
            if (node.GetStackCommand() != null)
            {
                node.GetStackCommand().Apply(this);
            }

            OutAStackOpCmd(node);
        }

        public virtual void InAReturnCmd(AReturnCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutAReturnCmd(AReturnCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseAReturnCmd(AReturnCmd node)
        {
            InAReturnCmd(node);
            if (node.GetReturn() != null)
            {
                node.GetReturn().Apply(this);
            }

            OutAReturnCmd(node);
        }

        public virtual void InAStoreStateCmd(AStoreStateCmd node)
        {
            DefaultIn(node);
        }

        public virtual void OutAStoreStateCmd(AStoreStateCmd node)
        {
            DefaultOut(node);
        }

        public override void CaseAStoreStateCmd(AStoreStateCmd node)
        {
            InAStoreStateCmd(node);
            if (node.GetStoreStateCommand() != null)
            {
                node.GetStoreStateCommand().Apply(this);
            }

            OutAStoreStateCmd(node);
        }

        public virtual void InAConditionalJumpCommand(AConditionalJumpCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutAConditionalJumpCommand(AConditionalJumpCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseAConditionalJumpCommand(AConditionalJumpCommand node)
        {
            InAConditionalJumpCommand(node);
            OutAConditionalJumpCommand(node);
        }

        public virtual void InAJumpCommand(AJumpCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutAJumpCommand(AJumpCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseAJumpCommand(AJumpCommand node)
        {
            InAJumpCommand(node);
            OutAJumpCommand(node);
        }

        public virtual void InAJumpToSubroutine(AJumpToSubroutine node)
        {
            DefaultIn(node);
        }

        public virtual void OutAJumpToSubroutine(AJumpToSubroutine node)
        {
            DefaultOut(node);
        }

        public override void CaseAJumpToSubroutine(AJumpToSubroutine node)
        {
            InAJumpToSubroutine(node);
            OutAJumpToSubroutine(node);
        }

        public virtual void InAReturn(AReturn node)
        {
            DefaultIn(node);
        }

        public virtual void OutAReturn(AReturn node)
        {
            DefaultOut(node);
        }

        public override void CaseAReturn(AReturn node)
        {
            InAReturn(node);
            OutAReturn(node);
        }

        public virtual void InACopyDownSpCommand(ACopyDownSpCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutACopyDownSpCommand(ACopyDownSpCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseACopyDownSpCommand(ACopyDownSpCommand node)
        {
            InACopyDownSpCommand(node);
            OutACopyDownSpCommand(node);
        }

        public virtual void InACopyTopSpCommand(ACopyTopSpCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutACopyTopSpCommand(ACopyTopSpCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseACopyTopSpCommand(ACopyTopSpCommand node)
        {
            InACopyTopSpCommand(node);
            OutACopyTopSpCommand(node);
        }

        public virtual void InACopyDownBpCommand(ACopyDownBpCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutACopyDownBpCommand(ACopyDownBpCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseACopyDownBpCommand(ACopyDownBpCommand node)
        {
            InACopyDownBpCommand(node);
            OutACopyDownBpCommand(node);
        }

        public virtual void InACopyTopBpCommand(ACopyTopBpCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutACopyTopBpCommand(ACopyTopBpCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseACopyTopBpCommand(ACopyTopBpCommand node)
        {
            InACopyTopBpCommand(node);
            OutACopyTopBpCommand(node);
        }

        public virtual void InAMoveSpCommand(AMoveSpCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutAMoveSpCommand(AMoveSpCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseAMoveSpCommand(AMoveSpCommand node)
        {
            InAMoveSpCommand(node);
            OutAMoveSpCommand(node);
        }

        public virtual void InARsaddCommand(ARsaddCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutARsaddCommand(ARsaddCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseARsaddCommand(ARsaddCommand node)
        {
            InARsaddCommand(node);
            OutARsaddCommand(node);
        }

        public virtual void InAConstCommand(AConstCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutAConstCommand(AConstCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseAConstCommand(AConstCommand node)
        {
            InAConstCommand(node);
            OutAConstCommand(node);
        }

        public virtual void InAActionCommand(AActionCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutAActionCommand(AActionCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseAActionCommand(AActionCommand node)
        {
            InAActionCommand(node);
            OutAActionCommand(node);
        }

        public virtual void InALogiiCommand(ALogiiCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutALogiiCommand(ALogiiCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseALogiiCommand(ALogiiCommand node)
        {
            InALogiiCommand(node);
            OutALogiiCommand(node);
        }

        public virtual void InABinaryCommand(ABinaryCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutABinaryCommand(ABinaryCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseABinaryCommand(ABinaryCommand node)
        {
            InABinaryCommand(node);
            OutABinaryCommand(node);
        }

        public virtual void InAUnaryCommand(AUnaryCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutAUnaryCommand(AUnaryCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseAUnaryCommand(AUnaryCommand node)
        {
            InAUnaryCommand(node);
            OutAUnaryCommand(node);
        }

        public virtual void InAStackCommand(AStackCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutAStackCommand(AStackCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseAStackCommand(AStackCommand node)
        {
            InAStackCommand(node);
            OutAStackCommand(node);
        }

        public virtual void InADestructCommand(ADestructCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutADestructCommand(ADestructCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseADestructCommand(ADestructCommand node)
        {
            InADestructCommand(node);
            OutADestructCommand(node);
        }

        public virtual void InABpCommand(ABpCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutABpCommand(ABpCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseABpCommand(ABpCommand node)
        {
            InABpCommand(node);
            OutABpCommand(node);
        }

        public virtual void InAStoreStateCommand(AStoreStateCommand node)
        {
            DefaultIn(node);
        }

        public virtual void OutAStoreStateCommand(AStoreStateCommand node)
        {
            DefaultOut(node);
        }

        public override void CaseAStoreStateCommand(AStoreStateCommand node)
        {
            InAStoreStateCommand(node);
            OutAStoreStateCommand(node);
        }

    }
}


