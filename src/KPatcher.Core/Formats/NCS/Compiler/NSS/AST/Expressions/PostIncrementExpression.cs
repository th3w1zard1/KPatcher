using System.Collections.Generic;
using System.Linq;
using KPatcher.Core.Common.Script;
using KPatcher.Core.Formats.NCS;
using KPatcher.Core.Formats.NCS.Compiler.NSS;

namespace KPatcher.Core.Formats.NCS.Compiler
{

    /// <summary>
    /// Represents a post-increment expression (x++).
    /// </summary>
    public class PostIncrementExpression : Expression
    {
        public FieldAccess FieldAccess { get; set; }

        public PostIncrementExpression(FieldAccess fieldAccess)
        {
            FieldAccess = fieldAccess ?? throw new System.ArgumentNullException(nameof(fieldAccess));
        }

        public override DynamicDataType Compile(NCS ncs, CodeRoot root, CodeBlock block)
        {
            DynamicDataType variableType = FieldAccess.Compile(ncs, root, block);
            block.TempStack += 4;

            if (variableType != DynamicDataType.INT)
            {
                string varName = string.Join(".", FieldAccess.Identifiers.Select(i => i.Label));
                throw new CompileError(
                    $"Increment operator (++) requires integer variable, got {variableType.Builtin.ToScriptString()}\n" +
                    $"  Variable: {varName}");
            }

            GetScopedResult scoped = FieldAccess.GetScoped(block, root);
            bool isGlobal = scoped.IsGlobal;
            int stackIndex = scoped.Offset;
            if (scoped.IsConst)
            {
                string varName = string.Join(".", FieldAccess.Identifiers.Select(i => i.Label));
                throw new NSS.CompileError($"Cannot increment const variable '{varName}'");
            }
            if (isGlobal)
            {
                ncs.Add(NCSInstructionType.INCxBP, new List<object> { stackIndex });
            }
            else
            {
                ncs.Add(NCSInstructionType.INCxSP, new List<object> { stackIndex });
            }

            block.TempStack -= 4;
            return variableType;
        }

        public override string ToString()
        {
            return $"{FieldAccess}++";
        }
    }
}

