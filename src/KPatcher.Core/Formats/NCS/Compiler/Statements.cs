using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using KPatcher.Core.Common.Script;

namespace KPatcher.Core.Formats.NCS.Compiler
{


    // Note: EmptyStatement, NopStatement, ExpressionStatement, DeclarationStatement are defined in NSS/AST/Statements/

    // Variable Declaration helper class (used by DeclarationStatement)

    public class VariableDeclarator
    {
        public Identifier Identifier { get; }
        [CanBeNull] public Expression Initializer { get; }

        public VariableDeclarator(Identifier identifier, [CanBeNull] Expression initializer = null)
        {
            Identifier = identifier;
            Initializer = initializer;
        }

        public void Compile(NCS ncs, CodeRoot root, CodeBlock block, DynamicDataType declaredType)
        {
            // Reserve stack space
            switch (declaredType.Builtin)
            {
                case DataType.Int: ncs.Add(NCSInstructionType.RSADDI); break;
                case DataType.Float: ncs.Add(NCSInstructionType.RSADDF); break;
                case DataType.String: ncs.Add(NCSInstructionType.RSADDS); break;
                case DataType.Object: ncs.Add(NCSInstructionType.RSADDO); break;
                case DataType.Vector:
                    ncs.Add(NCSInstructionType.RSADDF);
                    ncs.Add(NCSInstructionType.RSADDF);
                    ncs.Add(NCSInstructionType.RSADDF);
                    break;
                case DataType.Event: ncs.Add(NCSInstructionType.RSADDEVT); break;
                case DataType.Location: ncs.Add(NCSInstructionType.RSADDLOC); break;
                case DataType.Talent: ncs.Add(NCSInstructionType.RSADDTAL); break;
                case DataType.Effect: ncs.Add(NCSInstructionType.RSADDEFF); break;
                case DataType.Struct:
                    string structName = declaredType.Struct ?? throw new CompileError("Struct has no name");
                    root.StructMap[structName].Initialize(ncs, root);
                    break;
                default:
                    throw new CompileError($"Unsupported local variable type: {declaredType.Builtin}");
            }

            block.AddScoped(Identifier, declaredType);

            // Handle initializer
            if (Initializer != null)
            {
                // Save temp_stack before compiling expression
                int initialTempStack = block.TempStack;

                // Compile expression - expressions may or may not add to temp_stack themselves
                DynamicDataType initType = Initializer.Compile(ncs, root, block);
                int tempStackAfter = block.TempStack;

                // Only add to temp_stack if the expression didn't already add it
                if (tempStackAfter == initialTempStack)
                {
                    // Expression didn't add to temp_stack, so we need to add it
                    block.TempStack += initType.Size(root);
                }

                if (initType != declaredType)
                {
                    throw new CompileError($"Type mismatch in variable '{Identifier}' initializer");
                }

                // Get variable location - get_scoped uses temp_stack (including expression result) in its calculation
                GetScopedResult scoped = block.GetScoped(Identifier, root);
                NCSInstructionType instruction = scoped.IsGlobal ? NCSInstructionType.CPDOWNBP : NCSInstructionType.CPDOWNSP;
                // get_scoped() already accounts for temp_stack, so stack_index points to the correct variable location
                // We use scoped.Offset directly, not scoped.Offset - size
                ncs.Add(instruction, new List<object> { scoped.Offset, initType.Size(root) });

                // Assignment leaves result on stack, but VariableDeclarator is NOT in ExpressionStatement,
                // so we need to clean it up ourselves
                int resultSize = initType.Size(root);
                if (block.TempStack > initialTempStack)
                {
                    // Assignment left result on stack, remove it
                    ncs.Add(NCSInstructionType.MOVSP, new List<object> { -resultSize });
                    block.TempStack -= resultSize;
                }
            }
        }
    }


    // If / Else
    public class ConditionalBlock : Statement
    {
        public List<ConditionAndBlock> IfBlocks { get; }
        [CanBeNull] public CodeBlock ElseBlock { get; }

        public ConditionalBlock(List<ConditionAndBlock> ifBlocks, [CanBeNull] CodeBlock elseBlock)
        {
            IfBlocks = ifBlocks;
            ElseBlock = elseBlock;
        }

        public override object Compile(
            NCS ncs,
            [CanBeNull] CodeRoot root,
            CodeBlock block,
            NCSInstruction returnInstruction,
            NCSInstruction breakInstruction,
            [CanBeNull] NCSInstruction continueInstruction)
        {
            int jumpCount = 1 + IfBlocks.Count;
            var jumpTargets = new List<NCSInstruction>();
            for (int i = 0; i < jumpCount; i++)
            {
                jumpTargets.Add(new NCSInstruction(NCSInstructionType.NOP));
            }

            for (int i = 0; i < IfBlocks.Count; i++)
            {
                ConditionAndBlock branch = IfBlocks[i];
                // Save temp_stack state before condition
                int initialTempStack = block.TempStack;
                branch.Condition.Compile(ncs, root, block);
                // JZ consumes the condition value from stack
                ncs.Add(NCSInstructionType.JZ, jump: jumpTargets[i]);
                // Decrement temp_stack since JZ consumed the condition
                block.TempStack = initialTempStack;

                // Save temp_stack before compiling block
                int blockTempStackBefore = block.TempStack;
                branch.Block.Compile(ncs, root, block, returnInstruction, breakInstruction, continueInstruction);
                // Block should clear its own temp_stack, restore parent's temp_stack
                block.TempStack = blockTempStackBefore;
                ncs.Add(NCSInstructionType.JMP, jump: jumpTargets[jumpTargets.Count - 1]);

                ncs.Instructions.Add(jumpTargets[i]);
            }

            if (ElseBlock != null)
            {
                // Save temp_stack before compiling else block
                int elseTempStackBefore = block.TempStack;
                ElseBlock.Compile(ncs, root, block, returnInstruction, breakInstruction, continueInstruction);
                // Else block should clear its own temp_stack, restore parent's temp_stack
                block.TempStack = elseTempStackBefore;
            }

            ncs.Instructions.Add(jumpTargets[jumpTargets.Count - 1]);

            return DynamicDataType.VOID;
        }
    }

    public class ConditionAndBlock
    {
        public Expression Condition { get; }
        public CodeBlock Block { get; }

        public ConditionAndBlock(Expression condition, CodeBlock block)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            Block = block ?? throw new ArgumentNullException(nameof(block));
        }
    }

    // While / Do-While / For
    public class WhileLoopBlock : Statement
    {
        public Expression Condition { get; }
        public CodeBlock Block { get; }

        public WhileLoopBlock(Expression condition, CodeBlock block)
        {
            Condition = condition;
            Block = block;
        }

        public override object Compile(NCS ncs, [CanBeNull] CodeRoot root, CodeBlock block, NCSInstruction returnInstruction, NCSInstruction breakInstruction, [CanBeNull] NCSInstruction continueInstruction)
        {
            block.MarkBreakScope();

            NCSInstruction loopStart = ncs.Add(NCSInstructionType.NOP);
            var loopEnd = new NCSInstruction(NCSInstructionType.NOP);

            // Save temp_stack before condition (condition pushes a value, JZ consumes it)
            int initialTempStack = block.TempStack;
            DynamicDataType conditionType = Condition.Compile(ncs, root, block);

            if (conditionType != DynamicDataType.INT)
            {
                throw new CompileError(
                    $"Loop condition must be integer type, got {conditionType.Builtin.ToScriptString()}\n" +
                    $"  Note: Conditions must evaluate to int (0 = false, non-zero = true)");
            }

            // JZ consumes the condition value from stack
            ncs.Add(NCSInstructionType.JZ, jump: loopEnd);
            // Restore temp_stack since JZ consumed the condition
            block.TempStack = initialTempStack;

            Block.Compile(ncs, root, block, returnInstruction, loopEnd, loopStart);
            ncs.Add(NCSInstructionType.JMP, jump: loopStart);
            ncs.Instructions.Add(loopEnd);

            return DynamicDataType.VOID;
        }
    }

    public class DoWhileLoopBlock : Statement
    {
        public Expression Condition { get; }
        public CodeBlock Block { get; }

        public DoWhileLoopBlock(Expression condition, CodeBlock block)
        {
            Condition = condition;
            Block = block;
        }

        public override object Compile(NCS ncs, [CanBeNull] CodeRoot root, CodeBlock block, NCSInstruction returnInstruction, NCSInstruction breakInstruction, [CanBeNull] NCSInstruction continueInstruction)
        {
            block.MarkBreakScope();

            NCSInstruction loopStart = ncs.Add(NCSInstructionType.NOP);
            var conditionStart = new NCSInstruction(NCSInstructionType.NOP);
            var loopEnd = new NCSInstruction(NCSInstructionType.NOP);

            Block.Compile(ncs, root, block, returnInstruction, loopEnd, conditionStart);
            ncs.Instructions.Add(conditionStart);

            // Save temp_stack before condition (condition pushes a value, JZ consumes it)
            int initialTempStack = block.TempStack;
            DynamicDataType conditionType = Condition.Compile(ncs, root, block);
            if (conditionType != DynamicDataType.INT)
            {
                throw new CompileError(
                    $"do-while condition must be integer type, got {conditionType.Builtin.ToScriptString()}\n" +
                    $"  Note: Conditions must evaluate to int (0 = false, non-zero = true)");
            }

            // JZ consumes the condition value from stack
            ncs.Add(NCSInstructionType.JZ, jump: loopEnd);
            // Restore temp_stack since JZ consumed the condition
            block.TempStack = initialTempStack;

            ncs.Add(NCSInstructionType.JMP, jump: loopStart);
            ncs.Instructions.Add(loopEnd);

            return DynamicDataType.VOID;
        }
    }

    public class ForLoopBlock : Statement
    {
        public Expression Initializer { get; }
        public Expression Condition { get; }
        public Expression Iteration { get; }
        public CodeBlock Block { get; }

        public ForLoopBlock(Expression initializer, Expression condition, Expression iteration, CodeBlock block)
        {
            Initializer = initializer;
            Condition = condition;
            Iteration = iteration;
            Block = block;
        }

        public override object Compile(NCS ncs, [CanBeNull] CodeRoot root, CodeBlock block, NCSInstruction returnInstruction, NCSInstruction breakInstruction, [CanBeNull] NCSInstruction continueInstruction)
        {
            block.MarkBreakScope();

            // Handle initializer
            if (Initializer != null)
            {
                // For now, we only support Expression initializers in C#
                // If we need Statement support (for variable declarations), we'd need to check type
                int tempStackBefore = block.TempStack;
                DynamicDataType initType = Initializer.Compile(ncs, root, block);
                // Check if expression added to temp_stack
                if (block.TempStack == tempStackBefore)
                {
                    // Expression didn't add to temp_stack, so we need to add it
                    block.TempStack += initType.Size(root);
                }
                // Clean up the result from stack
                ncs.Add(NCSInstructionType.MOVSP, new List<object> { -initType.Size(root) });
                block.TempStack -= initType.Size(root);
            }

            NCSInstruction loopStart = ncs.Add(NCSInstructionType.NOP);
            var updateStart = new NCSInstruction(NCSInstructionType.NOP);
            var loopEnd = new NCSInstruction(NCSInstructionType.NOP);

            // Save temp_stack before condition (condition pushes a value, JZ consumes it)
            int initialTempStack = block.TempStack;
            DynamicDataType conditionType = Condition.Compile(ncs, root, block);
            if (conditionType != DynamicDataType.INT)
            {
                throw new CompileError(
                    $"for loop condition must be integer type, got {conditionType.Builtin.ToScriptString()}\n" +
                    $"  Note: Conditions must evaluate to int (0 = false, non-zero = true)");
            }

            // JZ consumes the condition value from stack
            ncs.Add(NCSInstructionType.JZ, jump: loopEnd);
            // Restore temp_stack since JZ consumed the condition
            block.TempStack = initialTempStack;

            Block.Compile(ncs, root, block, returnInstruction, loopEnd, updateStart);
            ncs.Instructions.Add(updateStart);

            int tempStackBeforeIteration = block.TempStack;
            DynamicDataType iterType = Iteration.Compile(ncs, root, block);
            int tempStackAfterIteration = block.TempStack;
            // Check if expression already added to temp_stack
            if (tempStackAfterIteration == tempStackBeforeIteration)
            {
                // Expression didn't add to temp_stack, so we need to add it
                block.TempStack += iterType.Size(root);
            }
            ncs.Add(NCSInstructionType.MOVSP, new List<object> { -iterType.Size(root) });
            block.TempStack -= iterType.Size(root);

            ncs.Add(NCSInstructionType.JMP, jump: loopStart);
            ncs.Instructions.Add(loopEnd);

            return DynamicDataType.VOID;
        }
    }

    // Note: BreakStatement and ContinueStatement are defined in NSS/AST/Statements/

}
