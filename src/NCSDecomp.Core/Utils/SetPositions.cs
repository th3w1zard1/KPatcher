// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS SetPositions.java.

using NCSDecomp.Core.Analysis;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Assigns bytecode positions to AST nodes (reversed walk).
    /// </summary>
    public sealed class SetPositions : PrunedReversedDepthFirstAdapter
    {
        private NodeAnalysisData nodedata;
        private int currentPos;

        public SetPositions(NodeAnalysisData nodedata)
        {
            this.nodedata = nodedata;
            currentPos = 0;
        }

        public void Done()
        {
            nodedata = null;
        }

        public override void DefaultIn(AstNode node)
        {
            int pos = NodeUtils.GetCommandPos(node);
            if (pos > 0)
            {
                currentPos = pos;
            }
        }

        public override void DefaultOut(AstNode node)
        {
            nodedata.SetPos(node, currentPos);
        }
    }
}
