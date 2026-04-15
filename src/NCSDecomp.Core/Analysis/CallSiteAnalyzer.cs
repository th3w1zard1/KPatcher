// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS CallSiteAnalyzer.java.

using System.Collections.Generic;
using NCSDecomp.Core;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.Utils;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Analysis
{
    /// <summary>
    /// Estimates JSR parameter counts from call-site stack growth (DeNCS CallSiteAnalyzer.java).
    /// </summary>
    public sealed class CallSiteAnalyzer : PrunedDepthFirstAdapter
    {
        private readonly NodeAnalysisData nodedata;
        private readonly SubroutineAnalysisData subdata;
        private readonly ActionsData actions;
        private readonly Dictionary<int, int> inferredParams = new Dictionary<int, int>();
        private bool skipdeadcode;
        private int height;
        private int growth;
        private SubroutineState state;

        public CallSiteAnalyzer(NodeAnalysisData nodedata, SubroutineAnalysisData subdata, ActionsData actions)
        {
            this.nodedata = nodedata;
            this.subdata = subdata;
            this.actions = actions;
        }

        /// <summary>
        /// Runs analysis across globals, main, and all subroutines; returns callee offset -> max inferred arg count.
        /// </summary>
        public Dictionary<int, int> Analyze()
        {
            if (subdata.GetGlobalsSub() != null)
            {
                AnalyzeSubroutine(subdata.GetGlobalsSub());
            }

            if (subdata.GetMainSub() != null)
            {
                AnalyzeSubroutine(subdata.GetMainSub());
            }

            foreach (ASubroutine sub in subdata.GetSubroutines())
            {
                AnalyzeSubroutine(sub);
            }

            return inferredParams;
        }

        private void AnalyzeSubroutine(ASubroutine sub)
        {
            state = subdata.GetState(sub);
            height = InitialHeight();
            growth = 0;
            skipdeadcode = false;
            sub.Apply(this);
        }

        private int InitialHeight()
        {
            int initial = 0;
            if (state != null)
            {
                if (!state.Type().Equals(DecompType.VtNone))
                {
                    initial++;
                }

                initial += state.GetParamCount();
            }

            return initial;
        }

        public override void DefaultIn(AstNode node)
        {
            if (NodeUtils.IsCommandNode(node))
            {
                skipdeadcode = !nodedata.ProcessCode(node);
            }
        }

        public override void OutARsaddCommand(ARsaddCommand node)
        {
            if (!skipdeadcode)
            {
                Push(1);
            }
        }

        public override void OutAConstCommand(AConstCommand node)
        {
            if (!skipdeadcode)
            {
                Push(1);
            }
        }

        public override void OutACopyTopSpCommand(ACopyTopSpCommand node)
        {
            if (!skipdeadcode)
            {
                Push(NodeUtils.StackSizeToPos(node.GetSize()));
            }
        }

        public override void OutACopyTopBpCommand(ACopyTopBpCommand node)
        {
            if (!skipdeadcode)
            {
                Push(NodeUtils.StackSizeToPos(node.GetSize()));
            }
        }

        public override void OutAActionCommand(AActionCommand node)
        {
            if (!skipdeadcode)
            {
                int remove = NodeUtils.ActionRemoveElementCount(node, actions);
                DecompType rettype = NodeUtils.GetReturnType(node, actions);
                int add;
                try
                {
                    add = NodeUtils.StackSizeToPos(rettype.TypeSize());
                }
                catch (System.InvalidOperationException)
                {
                    add = 1;
                }

                Pop(remove);
                Push(add);
            }
        }

        public override void OutALogiiCommand(ALogiiCommand node)
        {
            if (!skipdeadcode)
            {
                Pop(2);
                Push(1);
            }
        }

        public override void OutABinaryCommand(ABinaryCommand node)
        {
            if (!skipdeadcode)
            {
                int sizep1;
                int sizep2;
                int sizeresult;
                if (NodeUtils.IsEqualityOp(node))
                {
                    if (NodeUtils.GetType(node).Equals(DecompType.VtStructstruct))
                    {
                        sizep1 = NodeUtils.StackSizeToPos(node.GetSize());
                        sizep2 = sizep1;
                    }
                    else
                    {
                        sizep1 = 1;
                        sizep2 = 1;
                    }

                    sizeresult = 1;
                }
                else if (NodeUtils.IsVectorAllowedOp(node))
                {
                    sizep1 = NodeUtils.GetParam1Size(node);
                    sizep2 = NodeUtils.GetParam2Size(node);
                    sizeresult = NodeUtils.GetResultSize(node);
                }
                else
                {
                    sizep1 = 1;
                    sizep2 = 1;
                    sizeresult = 1;
                }

                Pop(sizep1 + sizep2);
                Push(sizeresult);
            }
        }

        public override void OutAConditionalJumpCommand(AConditionalJumpCommand node)
        {
            if (!skipdeadcode)
            {
                Pop(1);
            }
        }

        public override void OutAJumpCommand(AJumpCommand node)
        {
            if (!skipdeadcode && NodeUtils.GetJumpDestinationPos(node) < nodedata.GetPos(node))
            {
                ResetGrowth();
            }
        }

        public override void OutAJumpToSubroutine(AJumpToSubroutine node)
        {
            if (!skipdeadcode)
            {
                int dest = NodeUtils.GetJumpDestinationPos(node);
                int inferred = growth;
                if (inferred < 0)
                {
                    inferred = 0;
                }

                if (inferred > 0)
                {
                    inferred = inferred - 1;
                }

                if (inferred < 0)
                {
                    inferred = 0;
                }

                int existing;
                if (!inferredParams.TryGetValue(dest, out existing))
                {
                    existing = 0;
                }

                if (inferred > existing)
                {
                    inferredParams[dest] = inferred;
                }

                Pop(inferred);
                ResetGrowth();
            }
        }

        public override void OutAMoveSpCommand(AMoveSpCommand node)
        {
            if (!skipdeadcode)
            {
                Pop(NodeUtils.StackOffsetToPos(node.GetOffset()));
                ResetGrowth();
            }
        }

        public override void OutADestructCommand(ADestructCommand node)
        {
            if (!skipdeadcode)
            {
                Pop(NodeUtils.StackSizeToPos(node.GetSizeRem()));
                ResetGrowth();
            }
        }

        private void Push(int count)
        {
            if (count <= 0)
            {
                return;
            }

            height += count;
            growth += count;
        }

        private void Pop(int count)
        {
            if (count <= 0)
            {
                return;
            }

            height = height - count;
            if (height < 0)
            {
                height = 0;
            }

            growth = growth - count;
            if (growth < 0)
            {
                growth = 0;
            }
        }

        private void ResetGrowth()
        {
            growth = 0;
        }
    }
}
