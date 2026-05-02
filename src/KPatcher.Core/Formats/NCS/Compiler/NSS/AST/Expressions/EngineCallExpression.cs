using System;
using System.Collections.Generic;
using System.Linq;
using KPatcher.Core.Common;
using KPatcher.Core.Common.Script;
using KPatcher.Core.Formats.NCS;
using CompileError = KPatcher.Core.Formats.NCS.Compiler.NSS.CompileError;

namespace KPatcher.Core.Formats.NCS.Compiler
{

    /// <summary>
    /// Represents a call to an engine-defined function (e.g., SendMessageToPC, GetFirstPC).
    /// </summary>
    public class EngineCallExpression : Expression
    {
        public ScriptFunction Function { get; set; }
        public int RoutineId { get; set; }
        public List<Expression> Arguments { get; set; }

        public EngineCallExpression(ScriptFunction function, int routineId, List<Expression> arguments)
        {
            Function = function ?? throw new ArgumentNullException(nameof(function));
            RoutineId = routineId;
            Arguments = arguments ?? new List<Expression>();
        }

        public override DynamicDataType Compile(NCS ncs, CodeRoot root, CodeBlock block)
        {
            int argCount = Arguments.Count;

            if (argCount > Function.Params.Count)
            {
                throw new NSS.CompileError(
                    $"Too many arguments for '{Function.Name}'\n" +
                    $"  Expected: {Function.Params.Count}, Got: {argCount}");
            }

            // Fill in default parameters if needed
            for (int i = argCount; i < Function.Params.Count; i++)
            {
                ScriptParam param = Function.Params[i];
                if (param.Default == null)
                {
                    IEnumerable<string> requiredParams = Function.Params.Where(p => p.Default == null).Select(p => p.Name);
                    throw new NSS.CompileError(
                        $"Missing required arguments for '{Function.Name}'\n" +
                        $"  Required parameters: {string.Join(", ", requiredParams)}\n" +
                        $"  Provided: {argCount} argument(s)");
                }

                // [CanBeNull] Try to find constant
                ScriptConstant constant = root.FindConstant(param.Default.ToString() ?? "");
                if (constant != null)
                {
                    Arguments.Add(CreateConstantExpression(constant));
                }
                else
                {
                    // Parse default value based on parameter type
                    Arguments.Add(CreateDefaultExpression(param));
                }
            }

            // nwnnsscomp compiles `action` parameters before earlier parameters when any param is `action`
            // (e.g. DelayCommand body before the delay float), matching bytecode layout while keeping the
            // stack correct (last pushed = last param = top). Other engine calls use left-to-right order.
            bool reverseArgOrder = false;
            for (int p = 0; p < Function.Params.Count; p++)
            {
                if (Function.Params[p].DataType == DataType.Action)
                {
                    reverseArgOrder = true;
                    break;
                }
            }

            IEnumerable<int> argIndices = reverseArgOrder
                ? Enumerable.Range(0, Arguments.Count).Reverse()
                : Enumerable.Range(0, Arguments.Count);

            int thisStack = 0;
            foreach (int i in argIndices)
            {
                Expression arg = Arguments[i];
                ScriptParam param = Function.Params[i];
                var paramType = new DynamicDataType(param.DataType);

                if (paramType.Builtin == DataType.Action)
                {
                    // Special handling for action parameters (delayed execution)
                    NCSInstruction afterCommand = ncs.Add(NCSInstructionType.NOP, new List<object>());
                    ncs.Add(NCSInstructionType.STORE_STATE, new List<object>
                {
                    -root.ScopeSize(),
                    block.StackOffset + block.TempStack
                });
                    NCSInstruction jumpInst = ncs.Add(NCSInstructionType.JMP, new List<object>());
                    jumpInst.Jump = afterCommand;

                    arg.Compile(ncs, root, block);
                    ncs.Add(NCSInstructionType.RETN, new List<object>());
                }
                else
                {
                    int tempStackBefore = block.TempStack;
                    DynamicDataType addedType = arg.Compile(ncs, root, block);
                    // Only add to temp_stack if the expression didn't already add it
                    // (Some expressions like IdentifierExpression add their result, others like literals don't)
                    if (block.TempStack == tempStackBefore)
                    {
                        block.TempStack += addedType.Size(root);
                    }
                    thisStack += addedType.Size(root);

                    if (addedType.Builtin != paramType.Builtin)
                    {
                        throw new NSS.CompileError(
                            $"Type mismatch for parameter '{param.Name}' in call to '{Function.Name}'\n" +
                            $"  Expected: {paramType.Builtin.ToScriptString()}\n" +
                            $"  Got: {addedType.Builtin.ToScriptString()}");
                    }
                }
            }

            ncs.Add(NCSInstructionType.ACTION, new List<object> { RoutineId, Arguments.Count });
            block.TempStack -= thisStack;

            // For non-void functions, the return value is left on the stack
            // Add it to temp_stack so ExpressionStatement knows to pop it
            var returnType = new DynamicDataType(Function.ReturnType);
            if (returnType.Builtin != DataType.Void)
            {
                block.TempStack += returnType.Size(root);
            }

            return returnType;
        }

        private static Expression CreateConstantExpression(ScriptConstant constant)
        {
            switch (constant.DataType)
            {
                case DataType.Int:
                    return new IntExpression((int)constant.Value);
                case DataType.Float:
                    return new FloatExpression(Convert.ToSingle(constant.Value));
                case DataType.String:
                    return new StringExpression((string)constant.Value);
                case DataType.Object:
                    return new ObjectExpression((int)constant.Value);
                default:
                    throw new NSS.CompileError($"Unsupported constant type: {constant.DataType}");
            }
        }

        private Expression CreateDefaultExpression(ScriptParam param)
        {
            if (param.Default == null)
            {
                throw new NSS.CompileError($"Parameter '{param.Name}' has no default value");
            }

            switch (param.DataType)
            {
                case DataType.Int:
                    return new IntExpression(Convert.ToInt32(param.Default));
                case DataType.Float:
                    return new FloatExpression(Convert.ToSingle(param.Default));
                case DataType.String:
                    return new StringExpression(param.Default.ToString() ?? "");
                case DataType.Vector:
                    Vector3 vector = param.Default is Vector3 v
                        ? v
                        : new Vector3(0f, 0f, 0f);
                    return new VectorExpression(
                        new FloatExpression(vector.X),
                        new FloatExpression(vector.Y),
                        new FloatExpression(vector.Z));
                case DataType.Object:
                    if (param.Default is int objId)
                    {
                        return new ObjectExpression(objId);
                    }

                    string objDefault = param.Default.ToString()?.Trim() ?? "";
                    if (string.Equals(objDefault, "OBJECT_SELF", StringComparison.OrdinalIgnoreCase))
                    {
                        return new ObjectExpression(0);
                    }

                    if (string.Equals(objDefault, "OBJECT_INVALID", StringComparison.OrdinalIgnoreCase))
                    {
                        return new ObjectExpression(1);
                    }

                    if (int.TryParse(
                            objDefault,
                            System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out int parsedObj))
                    {
                        return new ObjectExpression(parsedObj);
                    }

                    throw new NSS.CompileError(
                        $"Unsupported object default value '{objDefault}' for parameter '{param.Name}' in '{Function.Name}'");
                default:
                    throw new NSS.CompileError(
                        $"Unsupported default parameter type '{param.DataType}' for '{param.Name}' in '{Function.Name}'\n" +
                        "  This may indicate a compiler limitation");
            }
        }

        public override string ToString()
        {
            string args = string.Join(", ", Arguments.Select(a => a.ToString()));
            return $"{Function.Name}({args})";
        }
    }
}

