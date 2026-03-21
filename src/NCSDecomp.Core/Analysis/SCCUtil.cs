// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;

namespace NCSDecomp.Core.Analysis
{
    /// <summary>
    /// Strongly-connected components for call-graph condensation (DeNCS SCCUtil.java).
    /// </summary>
    public static class SCCUtil
    {
        public static List<HashSet<int>> Compute(IReadOnlyDictionary<int, HashSet<int>> graph)
        {
            var tarjan = new Tarjan(graph);
            List<HashSet<int>> sccs = tarjan.Run();
            return TopologicalOrder(graph, sccs, tarjan.ComponentIndex);
        }

        private static List<HashSet<int>> TopologicalOrder(
            IReadOnlyDictionary<int, HashSet<int>> graph,
            List<HashSet<int>> sccs,
            Dictionary<int, int> compIndex)
        {
            var condensed = new Dictionary<int, HashSet<int>>();
            int[] indegree = new int[sccs.Count];
            foreach (KeyValuePair<int, HashSet<int>> entry in graph)
            {
                int fromComp = compIndex[entry.Key];
                foreach (int succ in entry.Value)
                {
                    int toComp = compIndex[succ];
                    if (fromComp != toComp)
                    {
                        if (!condensed.TryGetValue(fromComp, out HashSet<int> set))
                        {
                            set = new HashSet<int>();
                            condensed[fromComp] = set;
                        }

                        if (set.Add(toComp))
                        {
                            indegree[toComp]++;
                        }
                    }
                }
            }

            var queue = new Queue<int>();
            for (int i = 0; i < indegree.Length; i++)
            {
                if (indegree[i] == 0)
                {
                    queue.Enqueue(i);
                }
            }

            var ordered = new List<HashSet<int>>();
            while (queue.Count > 0)
            {
                int comp = queue.Dequeue();
                ordered.Add(sccs[comp]);
                if (condensed.TryGetValue(comp, out HashSet<int> succs))
                {
                    foreach (int succ in succs)
                    {
                        indegree[succ]--;
                        if (indegree[succ] == 0)
                        {
                            queue.Enqueue(succ);
                        }
                    }
                }
            }

            return ordered;
        }

        private sealed class Tarjan
        {
            private readonly IReadOnlyDictionary<int, HashSet<int>> graph;
            private readonly Dictionary<int, int> index = new Dictionary<int, int>();
            private readonly Dictionary<int, int> lowlink = new Dictionary<int, int>();
            private readonly Stack<int> stack = new Stack<int>();
            private readonly HashSet<int> onStack = new HashSet<int>();
            private readonly List<HashSet<int>> components = new List<HashSet<int>>();
            private int idx;
            public readonly Dictionary<int, int> ComponentIndex = new Dictionary<int, int>();

            public Tarjan(IReadOnlyDictionary<int, HashSet<int>> graph)
            {
                this.graph = graph;
            }

            public List<HashSet<int>> Run()
            {
                foreach (int node in graph.Keys)
                {
                    if (!index.ContainsKey(node))
                    {
                        StrongConnect(node);
                    }
                }

                return components;
            }

            private void StrongConnect(int v)
            {
                index[v] = idx;
                lowlink[v] = idx;
                idx++;
                stack.Push(v);
                onStack.Add(v);
                HashSet<int> succs;
                if (!graph.TryGetValue(v, out succs))
                {
                    succs = new HashSet<int>();
                }

                foreach (int w in succs)
                {
                    if (!index.ContainsKey(w))
                    {
                        StrongConnect(w);
                        lowlink[v] = Min(lowlink[v], lowlink[w]);
                    }
                    else if (onStack.Contains(w))
                    {
                        lowlink[v] = Min(lowlink[v], index[w]);
                    }
                }

                if (lowlink[v] == index[v])
                {
                    var component = new HashSet<int>();
                    int w;
                    do
                    {
                        w = stack.Pop();
                        onStack.Remove(w);
                        component.Add(w);
                        ComponentIndex[w] = components.Count;
                    }
                    while (w != v);
                    components.Add(component);
                }
            }

            private static int Min(int a, int b)
            {
                return a < b ? a : b;
            }
        }
    }
}
