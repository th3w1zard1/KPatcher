// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS DoGlobalVars.java.

using Microsoft.Extensions.Logging;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.Stack;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core
{
    /// <summary>
    /// MainPass variant for the globals subroutine (BP/stack freeze semantics).
    /// </summary>
    public sealed class DoGlobalVars : MainPass
    {
        private bool freezeStack;

        public DoGlobalVars(NodeAnalysisData nodedata, SubroutineAnalysisData subdata, ILogger log = null)
            : base(nodedata, subdata, log)
        {
            state.SetVarPrefix("GLOB_");
            freezeStack = false;
        }

        public new string GetCode()
        {
            return state.ToStringGlobals();
        }

        public override void OutABpCommand(ABpCommand node)
        {
            freezeStack = true;
        }

        public override void OutAJumpToSubroutine(AJumpToSubroutine node)
        {
            freezeStack = true;
        }

        public override void OutAMoveSpCommand(AMoveSpCommand node)
        {
            if (freezeStack)
            {
                return;
            }

            if (!skipdeadcode)
            {
                WithRecovery(node, () =>
                {
                    state.TransformMoveSp(node);
                    int remove = NodeUtils.StackOffsetToPos(node.GetOffset());
                    for (int i = 0; i < remove; i++)
                    {
                        stack.Remove();
                    }
                });
            }
            else
            {
                state.TransformDeadCode(node);
            }
        }

        public override void OutACopyDownSpCommand(ACopyDownSpCommand node)
        {
            if (freezeStack)
            {
                return;
            }

            base.OutACopyDownSpCommand(node);
        }

        public override void OutARsaddCommand(ARsaddCommand node)
        {
            if (freezeStack)
            {
                return;
            }

            base.OutARsaddCommand(node);
        }

        public LocalVarStack GetStack()
        {
            return stack;
        }
    }
}
