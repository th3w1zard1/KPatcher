using System.Collections.Generic;
using System.Linq;
using KPatcher.Core.Common.Script;
using KPatcher.Core.Formats.NCS;
using KPatcher.Core.Formats.NCS.Compiler.NSS;

namespace KPatcher.Core.Formats.NCS.Compiler
{

    /// <summary>
    /// Represents a pre-decrement expression (--x).
    /// </summary>
    public class PreDecrementExpression : Expression
    {
        public FieldAccess FieldAccess { get; set; }

        public PreDecrementExpression(FieldAccess fieldAccess)
        {
            FieldAccess = fieldAccess ?? throw new System.ArgumentNullException(nameof(fieldAccess));
        }

        public override DynamicDataType Compile(NCS ncs, CodeRoot root, CodeBlock block)
        {
            // First compile the field access to push value to stack
            // Note: FieldAccess.Compile does NOT add to temp_stack, so we don't either
            DynamicDataType variableType = FieldAccess.Compile(ncs, root, block);

            if (variableType.Builtin != DataType.Int)
            {
                string varName = string.Join(".", FieldAccess.Identifiers.Select(i => i.Label));
                throw new NSS.CompileError(
                    $"Decrement operator (--) requires integer variable, got {variableType.Builtin.ToScriptString().ToLower()}\n" +
                    $"  Variable: {varName}");
            }

            GetScopedResult scoped = FieldAccess.GetScoped(block, root);
            bool isGlobal = scoped.IsGlobal;
            int stackIndex = scoped.Offset;
            if (scoped.IsConst)
            {
                string varName = string.Join(".", FieldAccess.Identifiers.Select(i => i.Label));
                throw new NSS.CompileError($"Cannot decrement const variable '{varName}'");
            }

            // Decrement the value on the stack (the value that was just pushed by FieldAccess.Compile)
            // DECxSP with negative offset decrements the value at the top of the stack
            ncs.Add(NCSInstructionType.DECxSP, new List<object> { -variableType.Size(root) });

            // Copy the decremented value back to the variable location
            if (isGlobal)
            {
                ncs.Add(NCSInstructionType.CPDOWNBP, new List<object> { stackIndex, variableType.Size(root) });
            }
            else
            {
                ncs.Add(NCSInstructionType.CPDOWNSP, new List<object> { stackIndex - variableType.Size(root), variableType.Size(root) });
            }

            // The decremented value is still on the stack (for assignment), so temp_stack is correct
            return variableType;
        }

        public override string ToString()
        {
            return $"--{FieldAccess}";
        }
    }
}

