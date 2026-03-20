using System;
using System.Collections.Generic;
using KPatcher.Core.Common.Script;
using KPatcher.Core.Formats.NCS;

namespace KPatcher.Core.Formats.NCS.Compiler
{
    /// <summary>
    /// Represents a ternary conditional expression (condition ? trueExpr : falseExpr).
    /// </summary>
    public class TernaryConditionalExpression : Expression
    {
        public Expression Condition { get; set; }
        public Expression TrueExpression { get; set; }
        public Expression FalseExpression { get; set; }

        public TernaryConditionalExpression(
            Expression condition,
            Expression trueExpr,
            Expression falseExpr)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            TrueExpression = trueExpr ?? throw new ArgumentNullException(nameof(trueExpr));
            FalseExpression = falseExpr ?? throw new ArgumentNullException(nameof(falseExpr));
        }

        public override DynamicDataType Compile(NCS ncs, CodeRoot root, CodeBlock block)
        {
            int initialStack = block.TempStack;

            DynamicDataType conditionType = Condition.Compile(ncs, root, block);

            if (conditionType.Builtin != DataType.Int)
            {
                throw new NSS.CompileError(
                    $"Ternary condition must be integer type, got {conditionType.Builtin.ToScriptString()}\n" +
                    "  Note: Conditions must evaluate to int (0 = false, non-zero = true)");
            }

            // Jump to false branch if condition is zero (JZ consumes the condition from stack)
            NCSInstruction falseLabel = new NCSInstruction(NCSInstructionType.NOP, new List<object>());
            ncs.Add(NCSInstructionType.JZ, new List<object>(), falseLabel);

            block.TempStack = initialStack;

            DynamicDataType trueType = TrueExpression.Compile(ncs, root, block);
            block.TempStack += trueType.Size(root);

            NCSInstruction endLabel = new NCSInstruction(NCSInstructionType.NOP, new List<object>());
            ncs.Add(NCSInstructionType.JMP, new List<object>(), endLabel);

            // Stack state: same as after condition (condition was popped by JZ)
            ncs.Instructions.Add(falseLabel);

            block.TempStack = initialStack;

            DynamicDataType falseType = FalseExpression.Compile(ncs, root, block);
            // Explicitly track that false branch result is on the stack
            block.TempStack += falseType.Size(root);

            if (trueType.Builtin != falseType.Builtin)
            {
                throw new NSS.CompileError(
                    $"Type mismatch in ternary operator\n" +
                    $"  True branch type: {trueType.Builtin.ToScriptString()}\n" +
                    $"  False branch type: {falseType.Builtin.ToScriptString()}\n" +
                    "  Both branches must have the same type");
            }

            ncs.Instructions.Add(endLabel);

            block.TempStack = initialStack + trueType.Size(root);

            return trueType;
        }

        public override string ToString()
        {
            return $"({Condition} ? {TrueExpression} : {FalseExpression})";
        }
    }
}

