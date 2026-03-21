// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;
using NCSDecomp.Core.Analysis;
using NCSDecomp.Core.Node;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Detects globals block via BP ops (DeNCS CheckIsGlobals.java).
    /// </summary>
    public sealed class CheckIsGlobals : PrunedReversedDepthFirstAdapter
    {
        private bool isGlobals;

        public override void InABpCommand(ABpCommand node)
        {
            isGlobals = true;
        }

        public override void CaseACommandBlock(ACommandBlock node)
        {
            InACommandBlock(node);
            List<PCmd> temp = node.GetCmd().ToList();
            for (int i = temp.Count - 1; i >= 0; i--)
            {
                temp[i].Apply(this);
                if (isGlobals)
                {
                    return;
                }
            }

            OutACommandBlock(node);
        }

        public bool GetIsGlobals()
        {
            return isGlobals;
        }
    }
}
