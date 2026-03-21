// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS PrototypeEngine.java.

using System;
using System.Collections.Generic;
using NCSDecomp.Core;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.Analysis
{
    /// <summary>
    /// Runs subroutine prototyping in SCC / call-graph order (DeNCS PrototypeEngine.java).
    /// </summary>
    public sealed class PrototypeEngine
    {
        private const int MaxPasses = 3;
        private readonly NodeAnalysisData nodedata;
        private readonly SubroutineAnalysisData subdata;
        private readonly ActionsData actions;
        private readonly bool strict;

        public PrototypeEngine(NodeAnalysisData nodedata, SubroutineAnalysisData subdata, ActionsData actions, bool strict)
        {
            this.nodedata = nodedata;
            this.subdata = subdata;
            this.actions = actions;
            this.strict = strict;
        }

        public void Run()
        {
            CallGraphBuilder.CallGraph graph = new CallGraphBuilder(nodedata, subdata).Build();
            Dictionary<int, ASubroutine> subByPos = IndexSubroutines();
            int mainPos = nodedata.GetPos(subdata.GetMainSub());
            HashSet<int> reachable = graph.ReachableFrom(mainPos);
            if (subdata.GetGlobalsSub() != null)
            {
                foreach (int p in graph.ReachableFrom(nodedata.GetPos(subdata.GetGlobalsSub())))
                {
                    reachable.Add(p);
                }
            }

            List<HashSet<int>> sccs = SCCUtil.Compute(graph.Edges());
            foreach (HashSet<int> scc in sccs)
            {
                bool containsReachable = false;
                foreach (int p in scc)
                {
                    if (reachable.Contains(p))
                    {
                        containsReachable = true;
                        break;
                    }
                }

                if (!containsReachable)
                {
                    continue;
                }

                PrototypeComponent(scc, subByPos);
            }

            Dictionary<int, int> callsiteParams = new CallSiteAnalyzer(nodedata, subdata, actions).Analyze();
            EnsureAllPrototyped(subByPos.Values, callsiteParams);
        }

        private Dictionary<int, ASubroutine> IndexSubroutines()
        {
            var map = new Dictionary<int, ASubroutine>();
            foreach (ASubroutine sub in subdata.GetSubroutines())
            {
                map[nodedata.GetPos(sub)] = sub;
            }

            return map;
        }

        private void PrototypeComponent(HashSet<int> component, Dictionary<int, ASubroutine> subByPos)
        {
            var subs = new List<ASubroutine>();
            foreach (int pos in component)
            {
                ASubroutine sub;
                if (subByPos.TryGetValue(pos, out sub))
                {
                    subs.Add(sub);
                }
            }

            for (int pass = 0; pass < MaxPasses; pass++)
            {
                bool progress = false;
                foreach (ASubroutine sub in subs)
                {
                    SubroutineState state = subdata.GetState(sub);
                    if (state.IsPrototyped())
                    {
                        continue;
                    }

                    var finder = new SubroutinePathFinder(state, nodedata, subdata, pass);
                    sub.Apply(finder);
                    if (state.IsBeingPrototyped())
                    {
                        var dotypes = new DoTypes(state, nodedata, subdata, actions, true);
                        sub.Apply(dotypes);
                        dotypes.Done();
                        progress = progress || state.IsPrototyped();
                    }

                    finder.Done();
                }

                if (!progress)
                {
                    break;
                }
            }
        }

        private void EnsureAllPrototyped(IEnumerable<ASubroutine> subs, Dictionary<int, int> callsiteParams)
        {
            foreach (ASubroutine sub in subs)
            {
                SubroutineState state = subdata.GetState(sub);
                if (!state.IsPrototyped())
                {
                    if (strict)
                    {
                        Console.WriteLine("Strict signatures: missing prototype for subroutine at " + nodedata.GetPos(sub) + " (continuing)");
                    }

                    int pos = nodedata.GetPos(sub);
                    int inferredParams;
                    if (!callsiteParams.TryGetValue(pos, out inferredParams))
                    {
                        inferredParams = 0;
                    }

                    int movespParams = EstimateParamsFromMovesp(sub);
                    if (inferredParams > 0 && movespParams > 0)
                    {
                        inferredParams = Math.Min(inferredParams, movespParams);
                    }
                    else if (inferredParams == 0 && movespParams > 0)
                    {
                        inferredParams = movespParams;
                    }

                    if (inferredParams < 0)
                    {
                        inferredParams = 0;
                    }

                    state.StartPrototyping();
                    state.SetParamCount(inferredParams);
                    if (!state.Type().IsTyped())
                    {
                        state.SetReturnType(new DecompType(DecompType.VtInteger), 0);
                    }

                    state.EnsureParamPlaceholders();
                    state.StopPrototyping(true);
                }
            }
        }

        private int EstimateParamsFromMovesp(ASubroutine sub)
        {
            var scanner = new MoveSpParamScanner();
            sub.Apply(scanner);
            return scanner.MaxParams;
        }

        private sealed class MoveSpParamScanner : PrunedDepthFirstAdapter
        {
            public int MaxParams;

            public override void OutAMoveSpCommand(AMoveSpCommand node)
            {
                int @params = NodeUtils.StackOffsetToPos(node.GetOffset());
                if (@params > MaxParams)
                {
                    MaxParams = @params;
                }
            }
        }
    }
}
