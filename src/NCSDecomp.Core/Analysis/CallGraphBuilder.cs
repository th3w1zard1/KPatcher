// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.Utils;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Analysis
{
    /// <summary>
    /// Builds JSR call graph between subroutines (DeNCS CallGraphBuilder.java).
    /// </summary>
    public sealed class CallGraphBuilder : PrunedDepthFirstAdapter
    {
        private readonly NodeAnalysisData nodedata;
        private readonly SubroutineAnalysisData subdata;
        private readonly Dictionary<int, HashSet<int>> edges = new Dictionary<int, HashSet<int>>();
        private int current;

        public CallGraphBuilder(NodeAnalysisData nodedata, SubroutineAnalysisData subdata)
        {
            this.nodedata = nodedata;
            this.subdata = subdata;
        }

        public CallGraph Build()
        {
            if (subdata.GetGlobalsSub() != null)
            {
                subdata.GetGlobalsSub().Apply(this);
            }

            if (subdata.GetMainSub() != null)
            {
                subdata.GetMainSub().Apply(this);
            }

            foreach (ASubroutine sub in subdata.GetSubroutines())
            {
                sub.Apply(this);
            }

            return new CallGraph(edges);
        }

        public override void InASubroutine(ASubroutine node)
        {
            current = nodedata.GetPos(node);
            if (!edges.ContainsKey(current))
            {
                edges[current] = new HashSet<int>();
            }
        }

        public override void OutAJumpToSubroutine(AJumpToSubroutine node)
        {
            AstNode dest = nodedata.GetDestination(node);
            ASubroutine sub = dest as ASubroutine;
            if (sub != null)
            {
                int dstPos = nodedata.GetPos(sub);
                if (!edges.ContainsKey(current))
                {
                    edges[current] = new HashSet<int>();
                }

                edges[current].Add(dstPos);
            }
        }

        public sealed class CallGraph
        {
            private readonly Dictionary<int, HashSet<int>> forward;

            internal CallGraph(Dictionary<int, HashSet<int>> forward)
            {
                this.forward = new Dictionary<int, HashSet<int>>();
                foreach (KeyValuePair<int, HashSet<int>> e in forward)
                {
                    this.forward[e.Key] = new HashSet<int>(e.Value);
                }
            }

            public IReadOnlyDictionary<int, HashSet<int>> Edges()
            {
                return forward;
            }

            public HashSet<int> Successors(int node)
            {
                return forward.TryGetValue(node, out HashSet<int> s) ? s : new HashSet<int>();
            }

            public HashSet<int> Nodes()
            {
                return new HashSet<int>(forward.Keys);
            }

            public HashSet<int> ReachableFrom(int start)
            {
                var seen = new HashSet<int>();
                Dfs(start, seen);
                return seen;
            }

            private void Dfs(int node, HashSet<int> seen)
            {
                if (!seen.Add(node))
                {
                    return;
                }

                foreach (int succ in Successors(node))
                {
                    Dfs(succ, seen);
                }
            }
        }
    }
}
