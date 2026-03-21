// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Analysis;
using NCSDecomp.Core.Node;

namespace NCSDecomp.Core.Parser
{
    /// <summary>
    /// Maps token types to numeric indices for parser table lookups (SableCC).
    /// </summary>
    internal sealed class TokenIndex : AnalysisAdapter
    {
        internal int Index { get; set; }

        public override void CaseTLPar(TLPar node)
        {
            Index = 0;
        }

        public override void CaseTRPar(TRPar node)
        {
            Index = 1;
        }

        public override void CaseTSemi(TSemi node)
        {
            Index = 2;
        }

        public override void CaseTDot(TDot node)
        {
            Index = 3;
        }

        public override void CaseTCpdownsp(TCpdownsp node)
        {
            Index = 4;
        }

        public override void CaseTRsadd(TRsadd node)
        {
            Index = 5;
        }

        public override void CaseTCptopsp(TCptopsp node)
        {
            Index = 6;
        }

        public override void CaseTConst(TConst node)
        {
            Index = 7;
        }

        public override void CaseTAction(TAction node)
        {
            Index = 8;
        }

        public override void CaseTLogandii(TLogandii node)
        {
            Index = 9;
        }

        public override void CaseTLogorii(TLogorii node)
        {
            Index = 10;
        }

        public override void CaseTIncorii(TIncorii node)
        {
            Index = 11;
        }

        public override void CaseTExcorii(TExcorii node)
        {
            Index = 12;
        }

        public override void CaseTBoolandii(TBoolandii node)
        {
            Index = 13;
        }

        public override void CaseTEqual(TEqual node)
        {
            Index = 14;
        }

        public override void CaseTNequal(TNequal node)
        {
            Index = 15;
        }

        public override void CaseTGeq(TGeq node)
        {
            Index = 16;
        }

        public override void CaseTGt(TGt node)
        {
            Index = 17;
        }

        public override void CaseTLt(TLt node)
        {
            Index = 18;
        }

        public override void CaseTLeq(TLeq node)
        {
            Index = 19;
        }

        public override void CaseTShleft(TShleft node)
        {
            Index = 20;
        }

        public override void CaseTShright(TShright node)
        {
            Index = 21;
        }

        public override void CaseTUnright(TUnright node)
        {
            Index = 22;
        }

        public override void CaseTAdd(TAdd node)
        {
            Index = 23;
        }

        public override void CaseTSub(TSub node)
        {
            Index = 24;
        }

        public override void CaseTMul(TMul node)
        {
            Index = 25;
        }

        public override void CaseTDiv(TDiv node)
        {
            Index = 26;
        }

        public override void CaseTMod(TMod node)
        {
            Index = 27;
        }

        public override void CaseTNeg(TNeg node)
        {
            Index = 28;
        }

        public override void CaseTComp(TComp node)
        {
            Index = 29;
        }

        public override void CaseTMovsp(TMovsp node)
        {
            Index = 30;
        }

        public override void CaseTJmp(TJmp node)
        {
            Index = 31;
        }

        public override void CaseTJsr(TJsr node)
        {
            Index = 32;
        }

        public override void CaseTJz(TJz node)
        {
            Index = 33;
        }

        public override void CaseTRetn(TRetn node)
        {
            Index = 34;
        }

        public override void CaseTDestruct(TDestruct node)
        {
            Index = 35;
        }

        public override void CaseTNot(TNot node)
        {
            Index = 36;
        }

        public override void CaseTDecisp(TDecisp node)
        {
            Index = 37;
        }

        public override void CaseTIncisp(TIncisp node)
        {
            Index = 38;
        }

        public override void CaseTJnz(TJnz node)
        {
            Index = 39;
        }

        public override void CaseTCpdownbp(TCpdownbp node)
        {
            Index = 40;
        }

        public override void CaseTCptopbp(TCptopbp node)
        {
            Index = 41;
        }

        public override void CaseTDecibp(TDecibp node)
        {
            Index = 42;
        }

        public override void CaseTIncibp(TIncibp node)
        {
            Index = 43;
        }

        public override void CaseTSavebp(TSavebp node)
        {
            Index = 44;
        }

        public override void CaseTRestorebp(TRestorebp node)
        {
            Index = 45;
        }

        public override void CaseTStorestate(TStorestate node)
        {
            Index = 46;
        }

        public override void CaseTNop(TNop node)
        {
            Index = 47;
        }

        public override void CaseTT(TT node)
        {
            Index = 48;
        }

        public override void CaseTStringLiteral(TStringLiteral node)
        {
            Index = 49;
        }

        public override void CaseTIntegerConstant(TIntegerConstant node)
        {
            Index = 50;
        }

        public override void CaseTFloatConstant(TFloatConstant node)
        {
            Index = 51;
        }

        public override void CaseEOF(EOF node)
        {
            Index = 52;
        }
    }
}
