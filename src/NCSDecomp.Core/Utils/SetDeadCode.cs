// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS SetDeadCode.java.

using System.Collections;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;
using NCSDecomp.Core.Node;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Marks dead/unreachable control-flow using origins from <see cref="SetDestinations"/>.
    /// </summary>
    public sealed class SetDeadCode : PrunedDepthFirstAdapter
    {
        private NodeAnalysisData nodedata;
        private SubroutineAnalysisData subdata;
        private int actionarg;
        private Hashtable origins;
        private Hashtable deadorigins;
        private byte deadstate;
        private byte state;

        public SetDeadCode(NodeAnalysisData nodedata, SubroutineAnalysisData subdata, Hashtable origins)
        {
            this.nodedata = nodedata;
            this.origins = origins;
            this.subdata = subdata;
            actionarg = 0;
            deadstate = 0;
            state = 0;
            deadorigins = new Hashtable(1);
        }

        public void Done()
        {
            nodedata = null;
            subdata = null;
            origins = null;
            deadorigins = null;
        }

        public override void DefaultIn(AstNode node)
        {
            if (actionarg > 0 && origins != null && origins.ContainsKey(node))
            {
                actionarg--;
            }

            if (origins != null && origins.ContainsKey(node))
            {
                deadstate = 0;
            }
            else if (deadorigins.ContainsKey(node))
            {
                deadstate = 3;
            }

            if (NodeUtils.IsCommandNode(node))
            {
                nodedata.SetCodeState(node, deadstate);
            }
        }

        public override void DefaultOut(AstNode node)
        {
            if (NodeUtils.IsCommandNode(node))
            {
                state = 0;
            }
        }

        public override void OutAConditionalJumpCommand(AConditionalJumpCommand node)
        {
            if (deadstate == 1)
            {
                RemoveDestination(node, nodedata.GetDestination(node), origins);
            }
            else if (deadstate == 3)
            {
                TransferDestination(node, nodedata.GetDestination(node));
            }

            if (NodeUtils.IsJz(node))
            {
                if (state == 1)
                {
                    state++;
                    return;
                }

                if (state == 3)
                {
                    nodedata.LogOrCode(node, true);
                }
            }

            state = 0;
        }

        public override void OutACopyTopSpCommand(ACopyTopSpCommand node)
        {
            if (state != 0 && state != 2)
            {
                state = 0;
            }
            else
            {
                int copy = NodeUtils.StackSizeToPos(node.GetSize());
                int loc = NodeUtils.StackOffsetToPos(node.GetOffset());
                if (copy == 1 && loc == 1)
                {
                    state++;
                }
                else
                {
                    state = 0;
                }
            }
        }

        public override void OutAJumpCommand(AJumpCommand node)
        {
            if (deadstate == 1)
            {
                RemoveDestination(node, nodedata.GetDestination(node), origins);
            }
            else if (deadstate == 3)
            {
                TransferDestination(node, nodedata.GetDestination(node));
            }

            if (actionarg == 0)
            {
                deadstate = 3;
            }

            DefaultOut(node);
        }

        public override void OutAStoreStateCommand(AStoreStateCommand node)
        {
            actionarg++;
            DefaultOut(node);
        }

        public bool IsJumpToReturn(AJumpCommand node)
        {
            AstNode dest = nodedata.GetDestination(node);
            return dest is AReturn;
        }

        private void RemoveDestination(AstNode origin, AstNode destination)
        {
            RemoveDestination(origin, destination, origins);
        }

        private static void RemoveDestination(AstNode origin, AstNode destination, Hashtable hash)
        {
            if (hash == null || destination == null)
            {
                return;
            }

            var originlist = hash[destination] as ArrayList;
            if (originlist == null)
            {
                return;
            }

            originlist.Remove(origin);
            if (originlist.Count == 0)
            {
                hash.Remove(destination);
            }
        }

        private void TransferDestination(AstNode origin, AstNode destination)
        {
            RemoveDestination(origin, destination, origins);
            AddDestination(origin, destination, deadorigins);
        }

        private static void AddDestination(AstNode origin, AstNode destination, Hashtable hash)
        {
            var originslist = hash[destination] as ArrayList;
            if (originslist == null)
            {
                originslist = new ArrayList(1);
                hash[destination] = originslist;
            }

            originslist.Add(origin);
        }
    }
}
