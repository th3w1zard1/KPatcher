using System.Collections.Generic;
using JetBrains.Annotations;
using KPatcher.Core.Formats.NCS;

namespace KPatcher.Core.Formats.NCS.Compiler
{

    /// <summary>
    /// Represents a NOP (no operation) statement.
    /// </summary>
    public class NopStatement : Statement
    {
        public string String { get; }

        public NopStatement(string str)
        {
            String = str;
        }

        public override object Compile(
            NCS ncs,
            CodeRoot root,
            CodeBlock block,
            NCSInstruction returnInstruction,
            [CanBeNull] NCSInstruction breakInstruction,
            [CanBeNull] NCSInstruction continueInstruction)
        {
            ncs.Add(NCSInstructionType.NOP, new List<object> { String });
            return DynamicDataType.VOID;
        }
    }
}

