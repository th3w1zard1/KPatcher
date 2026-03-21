// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections;
using NCSDecomp.Core.Node;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Analysis
{
    /// <summary>
    /// Base visitor that provides empty implementations for all grammar callbacks.
    /// Subclasses override only the nodes they care about while still being able to
    /// store per-node state via in/out maps.
    /// </summary>
    public class AnalysisAdapter : IAnalysis
    {

        private Hashtable _in;
        private Hashtable _out;

        public object GetIn(AstNode node)
        {
            return _in == null ? null : _in[node];
        }

        public void SetIn(AstNode node, object value)
        {
            if (_in == null)
            {
                _in = new Hashtable();
            }

            if (value != null)
            {
                _in[node] = value;
            }
            else
            {
                _in.Remove(node);
            }
        }

        public object GetOut(AstNode node)
        {
            return _out == null ? null : _out[node];
        }

        public void SetOut(AstNode node, object value)
        {
            if (_out == null)
            {
                _out = new Hashtable();
            }

            if (value != null)
            {
                _out[node] = value;
            }
            else
            {
                _out.Remove(node);
            }
        }

        public virtual void CaseStart(Start node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAProgram(AProgram node)
        {
            DefaultCase(node);
        }

        public virtual void CaseASubroutine(ASubroutine node)
        {
            DefaultCase(node);
        }

        public virtual void CaseACommandBlock(ACommandBlock node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAAddVarCmd(AAddVarCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAActionJumpCmd(AActionJumpCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAConstCmd(AConstCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseACopydownspCmd(ACopydownspCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseACopytopspCmd(ACopytopspCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseACopydownbpCmd(ACopydownbpCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseACopytopbpCmd(ACopytopbpCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseACondJumpCmd(ACondJumpCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAJumpCmd(AJumpCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAJumpSubCmd(AJumpSubCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAMovespCmd(AMovespCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseALogiiCmd(ALogiiCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAUnaryCmd(AUnaryCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseABinaryCmd(ABinaryCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseADestructCmd(ADestructCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseABpCmd(ABpCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAActionCmd(AActionCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAStackOpCmd(AStackOpCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAReturnCmd(AReturnCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAStoreStateCmd(AStoreStateCmd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAAndLogiiOp(AAndLogiiOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAOrLogiiOp(AOrLogiiOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAInclOrLogiiOp(AInclOrLogiiOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAExclOrLogiiOp(AExclOrLogiiOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseABitAndLogiiOp(ABitAndLogiiOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAEqualBinaryOp(AEqualBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseANequalBinaryOp(ANequalBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAGeqBinaryOp(AGeqBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAGtBinaryOp(AGtBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseALtBinaryOp(ALtBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseALeqBinaryOp(ALeqBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAShrightBinaryOp(AShrightBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAShleftBinaryOp(AShleftBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAUnrightBinaryOp(AUnrightBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAAddBinaryOp(AAddBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseASubBinaryOp(ASubBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAMulBinaryOp(AMulBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseADivBinaryOp(ADivBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAModBinaryOp(AModBinaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseANegUnaryOp(ANegUnaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseACompUnaryOp(ACompUnaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseANotUnaryOp(ANotUnaryOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseADecispStackOp(ADecispStackOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAIncispStackOp(AIncispStackOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseADecibpStackOp(ADecibpStackOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAIncibpStackOp(AIncibpStackOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAIntConstant(AIntConstant node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAFloatConstant(AFloatConstant node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAStringConstant(AStringConstant node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAZeroJumpIf(AZeroJumpIf node)
        {
            DefaultCase(node);
        }

        public virtual void CaseANonzeroJumpIf(ANonzeroJumpIf node)
        {
            DefaultCase(node);
        }

        public virtual void CaseASavebpBpOp(ASavebpBpOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseARestorebpBpOp(ARestorebpBpOp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAConditionalJumpCommand(AConditionalJumpCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAJumpCommand(AJumpCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAJumpToSubroutine(AJumpToSubroutine node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAReturn(AReturn node)
        {
            DefaultCase(node);
        }

        public virtual void CaseACopyDownSpCommand(ACopyDownSpCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseACopyTopSpCommand(ACopyTopSpCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseACopyDownBpCommand(ACopyDownBpCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseACopyTopBpCommand(ACopyTopBpCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAMoveSpCommand(AMoveSpCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseARsaddCommand(ARsaddCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAConstCommand(AConstCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAActionCommand(AActionCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseALogiiCommand(ALogiiCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseABinaryCommand(ABinaryCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAUnaryCommand(AUnaryCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAStackCommand(AStackCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseADestructCommand(ADestructCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseABpCommand(ABpCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseAStoreStateCommand(AStoreStateCommand node)
        {
            DefaultCase(node);
        }

        public virtual void CaseASize(ASize node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTLPar(TLPar node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTRPar(TRPar node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTSemi(TSemi node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTDot(TDot node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTCpdownsp(TCpdownsp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTRsadd(TRsadd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTCptopsp(TCptopsp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTConst(TConst node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTAction(TAction node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTLogandii(TLogandii node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTLogorii(TLogorii node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTIncorii(TIncorii node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTExcorii(TExcorii node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTBoolandii(TBoolandii node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTEqual(TEqual node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTNequal(TNequal node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTGeq(TGeq node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTGt(TGt node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTLt(TLt node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTLeq(TLeq node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTShleft(TShleft node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTShright(TShright node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTUnright(TUnright node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTAdd(TAdd node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTSub(TSub node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTMul(TMul node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTDiv(TDiv node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTMod(TMod node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTNeg(TNeg node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTComp(TComp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTMovsp(TMovsp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTJmp(TJmp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTJsr(TJsr node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTJz(TJz node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTRetn(TRetn node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTDestruct(TDestruct node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTNot(TNot node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTDecisp(TDecisp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTIncisp(TIncisp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTJnz(TJnz node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTCpdownbp(TCpdownbp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTCptopbp(TCptopbp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTDecibp(TDecibp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTIncibp(TIncibp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTSavebp(TSavebp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTRestorebp(TRestorebp node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTStorestate(TStorestate node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTNop(TNop node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTT(TT node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTStringLiteral(TStringLiteral node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTBlank(TBlank node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTIntegerConstant(TIntegerConstant node)
        {
            DefaultCase(node);
        }

        public virtual void CaseTFloatConstant(TFloatConstant node)
        {
            DefaultCase(node);
        }

        public virtual void CaseEOF(EOF node)
        {
            DefaultCase(node);
        }

        public virtual void DefaultCase(AstNode node)
        {
        }
    }
}
