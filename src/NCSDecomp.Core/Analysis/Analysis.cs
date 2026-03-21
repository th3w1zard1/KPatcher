// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Node;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Analysis
{
    /// <summary>
    /// Visitor contract (SableCC analysis) for walking the decompiler AST.
    /// Named <see cref="IAnalysis"/> in C# because <c>NCSDecomp.Core.Analysis</c> is the containing namespace (Java: <c>Analysis</c>).
    /// </summary>
    public interface IAnalysis : Switch
    {
        object GetIn(AstNode node);
        void SetIn(AstNode node, object value);
        object GetOut(AstNode node);
        void SetOut(AstNode node, object value);
        void CaseStart(Start node);
        void CaseAProgram(AProgram node);
        void CaseASubroutine(ASubroutine node);
        void CaseACommandBlock(ACommandBlock node);
        void CaseAAddVarCmd(AAddVarCmd node);
        void CaseAActionJumpCmd(AActionJumpCmd node);
        void CaseAConstCmd(AConstCmd node);
        void CaseACopydownspCmd(ACopydownspCmd node);
        void CaseACopytopspCmd(ACopytopspCmd node);
        void CaseACopydownbpCmd(ACopydownbpCmd node);
        void CaseACopytopbpCmd(ACopytopbpCmd node);
        void CaseACondJumpCmd(ACondJumpCmd node);
        void CaseAJumpCmd(AJumpCmd node);
        void CaseAJumpSubCmd(AJumpSubCmd node);
        void CaseAMovespCmd(AMovespCmd node);
        void CaseALogiiCmd(ALogiiCmd node);
        void CaseAUnaryCmd(AUnaryCmd node);
        void CaseABinaryCmd(ABinaryCmd node);
        void CaseADestructCmd(ADestructCmd node);
        void CaseABpCmd(ABpCmd node);
        void CaseAActionCmd(AActionCmd node);
        void CaseAStackOpCmd(AStackOpCmd node);
        void CaseAReturnCmd(AReturnCmd node);
        void CaseAStoreStateCmd(AStoreStateCmd node);
        void CaseAAndLogiiOp(AAndLogiiOp node);
        void CaseAOrLogiiOp(AOrLogiiOp node);
        void CaseAInclOrLogiiOp(AInclOrLogiiOp node);
        void CaseAExclOrLogiiOp(AExclOrLogiiOp node);
        void CaseABitAndLogiiOp(ABitAndLogiiOp node);
        void CaseAEqualBinaryOp(AEqualBinaryOp node);
        void CaseANequalBinaryOp(ANequalBinaryOp node);
        void CaseAGeqBinaryOp(AGeqBinaryOp node);
        void CaseAGtBinaryOp(AGtBinaryOp node);
        void CaseALtBinaryOp(ALtBinaryOp node);
        void CaseALeqBinaryOp(ALeqBinaryOp node);
        void CaseAShrightBinaryOp(AShrightBinaryOp node);
        void CaseAShleftBinaryOp(AShleftBinaryOp node);
        void CaseAUnrightBinaryOp(AUnrightBinaryOp node);
        void CaseAAddBinaryOp(AAddBinaryOp node);
        void CaseASubBinaryOp(ASubBinaryOp node);
        void CaseAMulBinaryOp(AMulBinaryOp node);
        void CaseADivBinaryOp(ADivBinaryOp node);
        void CaseAModBinaryOp(AModBinaryOp node);
        void CaseANegUnaryOp(ANegUnaryOp node);
        void CaseACompUnaryOp(ACompUnaryOp node);
        void CaseANotUnaryOp(ANotUnaryOp node);
        void CaseADecispStackOp(ADecispStackOp node);
        void CaseAIncispStackOp(AIncispStackOp node);
        void CaseADecibpStackOp(ADecibpStackOp node);
        void CaseAIncibpStackOp(AIncibpStackOp node);
        void CaseAIntConstant(AIntConstant node);
        void CaseAFloatConstant(AFloatConstant node);
        void CaseAStringConstant(AStringConstant node);
        void CaseAZeroJumpIf(AZeroJumpIf node);
        void CaseANonzeroJumpIf(ANonzeroJumpIf node);
        void CaseASavebpBpOp(ASavebpBpOp node);
        void CaseARestorebpBpOp(ARestorebpBpOp node);
        void CaseAConditionalJumpCommand(AConditionalJumpCommand node);
        void CaseAJumpCommand(AJumpCommand node);
        void CaseAJumpToSubroutine(AJumpToSubroutine node);
        void CaseAReturn(AReturn node);
        void CaseACopyDownSpCommand(ACopyDownSpCommand node);
        void CaseACopyTopSpCommand(ACopyTopSpCommand node);
        void CaseACopyDownBpCommand(ACopyDownBpCommand node);
        void CaseACopyTopBpCommand(ACopyTopBpCommand node);
        void CaseAMoveSpCommand(AMoveSpCommand node);
        void CaseARsaddCommand(ARsaddCommand node);
        void CaseAConstCommand(AConstCommand node);
        void CaseAActionCommand(AActionCommand node);
        void CaseALogiiCommand(ALogiiCommand node);
        void CaseABinaryCommand(ABinaryCommand node);
        void CaseAUnaryCommand(AUnaryCommand node);
        void CaseAStackCommand(AStackCommand node);
        void CaseADestructCommand(ADestructCommand node);
        void CaseABpCommand(ABpCommand node);
        void CaseAStoreStateCommand(AStoreStateCommand node);
        void CaseASize(ASize node);
        void CaseTLPar(TLPar node);
        void CaseTRPar(TRPar node);
        void CaseTSemi(TSemi node);
        void CaseTDot(TDot node);
        void CaseTCpdownsp(TCpdownsp node);
        void CaseTRsadd(TRsadd node);
        void CaseTCptopsp(TCptopsp node);
        void CaseTConst(TConst node);
        void CaseTAction(TAction node);
        void CaseTLogandii(TLogandii node);
        void CaseTLogorii(TLogorii node);
        void CaseTIncorii(TIncorii node);
        void CaseTExcorii(TExcorii node);
        void CaseTBoolandii(TBoolandii node);
        void CaseTEqual(TEqual node);
        void CaseTNequal(TNequal node);
        void CaseTGeq(TGeq node);
        void CaseTGt(TGt node);
        void CaseTLt(TLt node);
        void CaseTLeq(TLeq node);
        void CaseTShleft(TShleft node);
        void CaseTShright(TShright node);
        void CaseTUnright(TUnright node);
        void CaseTAdd(TAdd node);
        void CaseTSub(TSub node);
        void CaseTMul(TMul node);
        void CaseTDiv(TDiv node);
        void CaseTMod(TMod node);
        void CaseTNeg(TNeg node);
        void CaseTComp(TComp node);
        void CaseTMovsp(TMovsp node);
        void CaseTJmp(TJmp node);
        void CaseTJsr(TJsr node);
        void CaseTJz(TJz node);
        void CaseTRetn(TRetn node);
        void CaseTDestruct(TDestruct node);
        void CaseTNot(TNot node);
        void CaseTDecisp(TDecisp node);
        void CaseTIncisp(TIncisp node);
        void CaseTJnz(TJnz node);
        void CaseTCpdownbp(TCpdownbp node);
        void CaseTCptopbp(TCptopbp node);
        void CaseTDecibp(TDecibp node);
        void CaseTIncibp(TIncibp node);
        void CaseTSavebp(TSavebp node);
        void CaseTRestorebp(TRestorebp node);
        void CaseTStorestate(TStorestate node);
        void CaseTNop(TNop node);
        void CaseTT(TT node);
        void CaseTStringLiteral(TStringLiteral node);
        void CaseTBlank(TBlank node);
        void CaseTIntegerConstant(TIntegerConstant node);
        void CaseTFloatConstant(TFloatConstant node);
        void CaseEOF(EOF node);
    }
}
