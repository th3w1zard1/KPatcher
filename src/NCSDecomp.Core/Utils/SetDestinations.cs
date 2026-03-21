// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS SetDestinations.java.

using System.Collections;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;
using NCSDecomp.Core.Node;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Resolves jump targets to AST nodes and records origin lists for dead-code analysis.
    /// </summary>
    public sealed class SetDestinations : PrunedDepthFirstAdapter
    {
        private NodeAnalysisData nodedata;
        private SubroutineAnalysisData subdata;
        private AstNode destination;
        private readonly AstNode astRoot;
        private readonly Hashtable origins = new Hashtable(1);

        public SetDestinations(AstNode ast, NodeAnalysisData nodedata, SubroutineAnalysisData subdata)
        {
            this.nodedata = nodedata;
            astRoot = ast;
            this.subdata = subdata;
        }

        public void Done()
        {
            nodedata = null;
            subdata = null;
            destination = null;
        }

        public Hashtable GetOrigins()
        {
            return origins;
        }

        public override void OutAConditionalJumpCommand(AConditionalJumpCommand node)
        {
            int pos = NodeUtils.GetJumpDestinationPos(node);
            LookForPos(pos, true);
            if (destination == null)
            {
                throw new System.InvalidOperationException("wasn't able to find dest for " + node + " at pos " + pos);
            }

            nodedata.SetDestination(node, destination);
            AddDestination(node, destination);
        }

        public override void OutAJumpCommand(AJumpCommand node)
        {
            int pos = NodeUtils.GetJumpDestinationPos(node);
            LookForPos(pos, true);
            if (destination == null)
            {
                throw new System.InvalidOperationException("wasn't able to find dest for " + node + " at pos " + pos);
            }

            nodedata.SetDestination(node, destination);
            if (pos < nodedata.GetPos(node))
            {
                AstNode dest = NodeUtils.GetCommandChild(destination);
                nodedata.AddOrigin(dest, node);
            }

            AddDestination(node, destination);
        }

        public override void OutAJumpToSubroutine(AJumpToSubroutine node)
        {
            int pos = NodeUtils.GetJumpDestinationPos(node);
            LookForPos(pos, false);
            if (destination == null)
            {
                throw new System.InvalidOperationException("wasn't able to find dest for " + node + " at pos " + pos);
            }

            nodedata.SetDestination(node, destination);
            AddDestination(node, destination);
        }

        private void AddDestination(AstNode origin, AstNode dest)
        {
            var originslist = origins[dest] as ArrayList;
            if (originslist == null)
            {
                originslist = new ArrayList(1);
                origins[dest] = originslist;
            }

            originslist.Add(origin);
        }

        private int GetPos(AstNode node)
        {
            return nodedata.GetPos(node);
        }

        private void LookForPos(int pos, bool needcommand)
        {
            destination = null;
            var search = new DestinationSearch(this, pos, needcommand);
            astRoot.Apply(search);
        }

        private sealed class DestinationSearch : PrunedDepthFirstAdapter
        {
            private readonly SetDestinations outer;
            private readonly int pos;
            private readonly bool needcommand;

            public DestinationSearch(SetDestinations outer, int pos, bool needcommand)
            {
                this.outer = outer;
                this.pos = pos;
                this.needcommand = needcommand;
            }

            public override void DefaultIn(AstNode node)
            {
                if (outer.destination == null && outer.GetPos(node) == pos &&
                    (!needcommand || NodeUtils.IsCommandNode(node)))
                {
                    outer.destination = node;
                }
            }

            public override void CaseAProgram(AProgram node)
            {
                InAProgram(node);
                if (node.GetReturn() != null)
                {
                    node.GetReturn().Apply(this);
                }

                List<PSubroutine> temp = node.GetSubroutine().ToList();
                int cur = temp.Count / 2;
                int min = 0;
                int max = temp.Count - 1;

                for (bool done = outer.destination != null || cur >= temp.Count;
                     !done;
                     done = done || outer.destination != null || cur > max)
                {
                    PSubroutine sub = temp[cur];
                    if (outer.GetPos(sub) > pos)
                    {
                        max = cur;
                        cur = (min + cur) / 2;
                    }
                    else if (outer.GetPos(sub) == pos)
                    {
                        sub.Apply(this);
                        done = true;
                    }
                    else if (cur >= max - 1)
                    {
                        sub.Apply(this);
                        cur++;
                    }
                    else
                    {
                        min = cur;
                        cur = (cur + max) / 2;
                    }
                }

                OutAProgram(node);
            }

            public override void CaseACommandBlock(ACommandBlock node)
            {
                InACommandBlock(node);
                List<PCmd> temp = node.GetCmd().ToList();
                int cur = temp.Count / 2;
                int min = 0;
                int max = temp.Count - 1;

                for (bool done = outer.destination != null || cur >= temp.Count;
                     !done;
                     done = done || outer.destination != null || cur > max)
                {
                    PCmd cmd = temp[cur];
                    if (outer.GetPos(cmd) > pos)
                    {
                        max = cur;
                        cur = (min + cur) / 2;
                    }
                    else if (outer.GetPos(cmd) == pos)
                    {
                        cmd.Apply(this);
                        done = true;
                    }
                    else if (cur >= max - 1)
                    {
                        cmd.Apply(this);
                        cur++;
                    }
                    else
                    {
                        min = cur;
                        cur = (cur + max) / 2;
                    }
                }

                OutACommandBlock(node);
            }
        }
    }
}
