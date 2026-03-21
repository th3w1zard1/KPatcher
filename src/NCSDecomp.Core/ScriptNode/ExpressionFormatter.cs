// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

namespace NCSDecomp.Core.ScriptNode
{
    /// <summary>
    /// Centralized expression pretty-printer (DeNCS ExpressionFormatter.java).
    /// </summary>
    internal static class ExpressionFormatter
    {
        private enum Position
        {
            None,
            Left,
            Right
        }

        private const int PrecAssignment = 1;
        private const int PrecLogicalOr = 2;
        private const int PrecLogicalAnd = 3;
        private const int PrecBitOr = 4;
        private const int PrecBitXor = 5;
        private const int PrecBitAnd = 6;
        private const int PrecEquality = 7;
        private const int PrecRelational = 8;
        private const int PrecShift = 9;
        private const int PrecAdditive = 10;
        private const int PrecMultiplicative = 11;
        private const int PrecUnary = 12;

        internal static string Format(IAExpression expr)
        {
            return Format(expr, int.MaxValue, Position.None, null);
        }

        internal static string FormatValue(IAExpression expr)
        {
            string rendered = Format(expr);
            if (expr is ABinaryExp bexp && NeedsValueParens(bexp))
            {
                return EnsureWrapped(rendered);
            }

            return rendered;
        }

        private static string Format(IAExpression expr, int parentPrec, Position side, string parentOp)
        {
            if (expr == null)
            {
                return "";
            }

            if (expr is ABinaryExp b)
            {
                return FormatBinary(b, parentPrec, side, parentOp);
            }

            if (expr is AConditionalExp c)
            {
                return FormatConditional(c, parentPrec, side, parentOp);
            }

            if (expr is AUnaryExp u)
            {
                return FormatUnary(u, parentPrec, side, parentOp);
            }

            if (expr is AUnaryModExp um)
            {
                return FormatUnaryMod(um, parentPrec, side, parentOp);
            }

            if (expr is AModifyExp m)
            {
                return FormatAssignment(m, parentPrec, side, parentOp);
            }

            return expr.ToString();
        }

        private static string FormatBinary(ABinaryExp exp, int parentPrec, Position side, string parentOp)
        {
            string op = exp.Op();
            int prec = Precedence(op);
            string left = Format(exp.Left(), prec, Position.Left, op);
            string right = Format(exp.Right(), prec, Position.Right, op);
            string rendered = left + " " + op + " " + right;
            return WrapIfNeeded(rendered, prec, parentPrec, side, parentOp, op);
        }

        private static string FormatConditional(AConditionalExp exp, int parentPrec, Position side, string parentOp)
        {
            string op = exp.Op();
            int prec = Precedence(op);
            string left = Format(exp.Left(), prec, Position.Left, op);
            string right = Format(exp.Right(), prec, Position.Right, op);
            string rendered = left + " " + op + " " + right;
            if (exp.ForceParens())
            {
                return "(" + rendered + ")";
            }

            return WrapIfNeeded(rendered, prec, parentPrec, side, parentOp, op);
        }

        private static string FormatUnary(AUnaryExp exp, int parentPrec, Position side, string parentOp)
        {
            string op = exp.Op();
            int prec = PrecUnary;
            string inner = Format(exp.Exp(), prec, Position.Right, op);
            string rendered = op + inner;
            return WrapIfNeeded(rendered, prec, parentPrec, side, parentOp, op);
        }

        private static string FormatUnaryMod(AUnaryModExp exp, int parentPrec, Position side, string parentOp)
        {
            string op = exp.Op();
            int prec = PrecUnary;
            string target = Format(exp.VarRef(), prec, Position.Right, op);
            string rendered = exp.Prefix() ? op + target : target + op;
            return WrapIfNeeded(rendered, prec, parentPrec, side, parentOp, op);
        }

        private static string FormatAssignment(AModifyExp exp, int parentPrec, Position side, string parentOp)
        {
            int prec = PrecAssignment;
            string left = Format(exp.VarRef(), prec, Position.Left, "=");
            string right = Format(exp.Expression(), prec, Position.Right, "=");
            string rendered = left + " = " + right;
            return WrapIfNeeded(rendered, prec, parentPrec, side, parentOp, "=");
        }

        private static string WrapIfNeeded(string rendered, int selfPrec, int parentPrec, Position side, string parentOp,
            string selfOp)
        {
            return ShouldParenthesize(selfPrec, parentPrec, side, parentOp, selfOp) ? "(" + rendered + ")" : rendered;
        }

        private static bool ShouldParenthesize(int selfPrec, int parentPrec, Position side, string parentOp,
            string selfOp)
        {
            if (parentPrec == int.MaxValue)
            {
                return false;
            }

            if (selfPrec < parentPrec)
            {
                return true;
            }

            if (selfPrec > parentPrec || parentOp == null)
            {
                return false;
            }

            if (side == Position.Right)
            {
                if (IsNonAssociative(parentOp))
                {
                    return true;
                }

                return parentOp != selfOp;
            }

            return false;
        }

        private static int Precedence(string op)
        {
            if (op == null)
            {
                return PrecUnary;
            }

            switch (op)
            {
                case "||":
                    return PrecLogicalOr;
                case "&&":
                    return PrecLogicalAnd;
                case "|":
                    return PrecBitOr;
                case "^":
                    return PrecBitXor;
                case "&":
                    return PrecBitAnd;
                case "==":
                case "!=":
                    return PrecEquality;
                case "<":
                case "<=":
                case ">":
                case ">=":
                    return PrecRelational;
                case "<<":
                case ">>":
                    return PrecShift;
                case "+":
                case "-":
                    return PrecAdditive;
                case "*":
                case "/":
                case "%":
                    return PrecMultiplicative;
                default:
                    return PrecUnary;
            }
        }

        private static bool IsNonAssociative(string op)
        {
            if (op == null)
            {
                return false;
            }

            switch (op)
            {
                case "=":
                case "-":
                case "/":
                case "%":
                case "<<":
                case ">>":
                case "==":
                case "!=":
                case "<":
                case "<=":
                case ">":
                case ">=":
                    return true;
                default:
                    return false;
            }
        }

        private static bool NeedsValueParens(ABinaryExp exp)
        {
            return IsComparisonOp(exp.Op());
        }

        private static bool IsComparisonOp(string op)
        {
            if (op == null)
            {
                return false;
            }

            switch (op)
            {
                case "==":
                case "!=":
                case "<":
                case "<=":
                case ">":
                case ">=":
                    return true;
                default:
                    return false;
            }
        }

        private static string EnsureWrapped(string rendered)
        {
            string trimmed = rendered.Trim();
            return trimmed.StartsWith("(") && trimmed.EndsWith(")") ? rendered : "(" + rendered + ")";
        }
    }
}
