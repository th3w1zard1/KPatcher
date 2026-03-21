// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;
using NCSDecomp.Core.Stack;

namespace NCSDecomp.Core.ScriptNode
{
    public class AActionArgExp : ScriptRootNode, IAExpression
    {
        public AActionArgExp(int start, int end)
            : base(start, end)
        {
            this.start = start;
            this.end = end;
        }

        public override string ToString()
        {
            if (children == null || children.Count == 0)
            {
                return "/*action*/";
            }

            for (LinkedListNode<ScriptNode> n = children.Last; n != null; n = n.Previous)
            {
                ScriptNode child = n.Value;
                if (child is AExpressionStatement es)
                {
                    IAExpression exp = es.Exp();
                    return exp != null ? exp.ToString() : "/*action*/";
                }

                if (child is IAExpression)
                {
                    return child.ToString();
                }
            }

            return "/*action*/";
        }

        public StackEntry Stackentry()
        {
            return null;
        }

        public void Stackentry(StackEntry e)
        {
        }
    }
}
