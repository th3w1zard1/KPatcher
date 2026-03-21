// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS SubroutinePathFinder.java.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;
using NCSDecomp.Core.Node;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Traces control-flow paths inside a subroutine for prototype discovery (DeNCS SubroutinePathFinder.java).
    /// </summary>
    public sealed class SubroutinePathFinder : PrunedDepthFirstAdapter
    {
        private NodeAnalysisData nodedata;
        private SubroutineAnalysisData subdata;
        private SubroutineState state;
        private bool pathfailed;
        private bool forcejump;
        private Dictionary<int, int> destinationcommands;
        private readonly bool limitretries;
        private readonly int maxretry;
        private int retry;

        public SubroutinePathFinder(SubroutineState state, NodeAnalysisData nodedata, SubroutineAnalysisData subdata, int pass)
        {
            this.nodedata = nodedata;
            this.subdata = subdata;
            this.state = state;
            pathfailed = false;
            forcejump = false;
            limitretries = pass < 3;
            switch (pass)
            {
                case 0:
                    maxretry = 10;
                    break;
                case 1:
                    maxretry = 15;
                    break;
                case 2:
                    maxretry = 25;
                    break;
                default:
                    maxretry = 9999;
                    break;
            }

            retry = 0;
        }

        public void Done()
        {
            nodedata = null;
            subdata = null;
            state = null;
            destinationcommands = null;
        }

        public override void InASubroutine(ASubroutine node)
        {
            state.StartPrototyping();
        }

        public override void CaseACommandBlock(ACommandBlock node)
        {
            InACommandBlock(node);
            List<PCmd> commands = node.GetCmd().ToList();
            SetupDestinationCommands(commands, node);
            int i = 0;
            while (i < commands.Count)
            {
                if (forcejump)
                {
                    int nextPos = state.GetCurrentDestination();
                    i = destinationcommands[nextPos];
                    forcejump = false;
                }
                else if (pathfailed)
                {
                    int nextPos = state.SwitchDecision();
                    if (nextPos == -1 || (limitretries && retry > maxretry))
                    {
                        state.StopPrototyping(false);
                        return;
                    }

                    i = destinationcommands[nextPos];
                    pathfailed = false;
                    retry++;
                }

                if (i < commands.Count)
                {
                    commands[i].Apply(this);
                    i++;
                }
            }

            OutACommandBlock(node);
        }

        public override void OutAConditionalJumpCommand(AConditionalJumpCommand node)
        {
            NodeUtils.GetNextCommand(node, nodedata);
            if (!nodedata.LogOrCode(node))
            {
                state.AddDecision(node, NodeUtils.GetJumpDestinationPos(node));
            }
        }

        public override void OutAJumpCommand(AJumpCommand node)
        {
            if (NodeUtils.GetJumpDestinationPos(node) < nodedata.GetPos(node))
            {
                pathfailed = true;
            }
            else
            {
                state.AddJump(node, NodeUtils.GetJumpDestinationPos(node));
                forcejump = true;
            }
        }

        public override void OutAJumpToSubroutine(AJumpToSubroutine node)
        {
            if (!subdata.IsPrototyped(NodeUtils.GetJumpDestinationPos(node), true))
            {
                pathfailed = true;
            }
        }

        public override void CaseAAddVarCmd(AAddVarCmd node)
        {
        }

        public override void CaseAConstCmd(AConstCmd node)
        {
        }

        public override void CaseACopydownspCmd(ACopydownspCmd node)
        {
        }

        public override void CaseACopytopspCmd(ACopytopspCmd node)
        {
        }

        public override void CaseACopydownbpCmd(ACopydownbpCmd node)
        {
        }

        public override void CaseACopytopbpCmd(ACopytopbpCmd node)
        {
        }

        public override void CaseAMovespCmd(AMovespCmd node)
        {
        }

        public override void CaseALogiiCmd(ALogiiCmd node)
        {
        }

        public override void CaseAUnaryCmd(AUnaryCmd node)
        {
        }

        public override void CaseABinaryCmd(ABinaryCmd node)
        {
        }

        public override void CaseADestructCmd(ADestructCmd node)
        {
        }

        public override void CaseABpCmd(ABpCmd node)
        {
        }

        public override void CaseAActionCmd(AActionCmd node)
        {
        }

        public override void CaseAStackOpCmd(AStackOpCmd node)
        {
        }

        private void SetupDestinationCommands(List<PCmd> commands, AstNode ast)
        {
            destinationcommands = new Dictionary<int, int>();
            ast.Apply(new DestinationCommandsSetup(this, commands));
        }

        private int GetCommandIndexByPos(int pos, List<PCmd> commands)
        {
            AstNode cmdnode = commands[0];
            int i;
            for (i = 1; i < commands.Count && nodedata.GetPos(cmdnode) < pos; i++)
            {
                cmdnode = commands[i];
                if (nodedata.GetPos(cmdnode) == pos)
                {
                    break;
                }
            }

            if (nodedata.GetPos(cmdnode) > pos)
            {
                throw new InvalidOperationException("Unable to locate a command with position " + pos);
            }

            return i;
        }

        private sealed class DestinationCommandsSetup : PrunedDepthFirstAdapter
        {
            private readonly SubroutinePathFinder outer;
            private readonly List<PCmd> commands;

            public DestinationCommandsSetup(SubroutinePathFinder outer, List<PCmd> commands)
            {
                this.outer = outer;
                this.commands = commands;
            }

            public override void OutAConditionalJumpCommand(AConditionalJumpCommand node)
            {
                int pos = NodeUtils.GetJumpDestinationPos(node);
                outer.destinationcommands[pos] = outer.GetCommandIndexByPos(pos, commands);
            }

            public override void OutAJumpCommand(AJumpCommand node)
            {
                int pos = NodeUtils.GetJumpDestinationPos(node);
                outer.destinationcommands[pos] = outer.GetCommandIndexByPos(pos, commands);
            }

            public override void CaseAAddVarCmd(AAddVarCmd node)
            {
            }

            public override void CaseAConstCmd(AConstCmd node)
            {
            }

            public override void CaseACopydownspCmd(ACopydownspCmd node)
            {
            }

            public override void CaseACopytopspCmd(ACopytopspCmd node)
            {
            }

            public override void CaseACopydownbpCmd(ACopydownbpCmd node)
            {
            }

            public override void CaseACopytopbpCmd(ACopytopbpCmd node)
            {
            }

            public override void CaseAMovespCmd(AMovespCmd node)
            {
            }

            public override void CaseALogiiCmd(ALogiiCmd node)
            {
            }

            public override void CaseAUnaryCmd(AUnaryCmd node)
            {
            }

            public override void CaseABinaryCmd(ABinaryCmd node)
            {
            }

            public override void CaseADestructCmd(ADestructCmd node)
            {
            }

            public override void CaseABpCmd(ABpCmd node)
            {
            }

            public override void CaseAActionCmd(AActionCmd node)
            {
            }

            public override void CaseAStackOpCmd(AStackOpCmd node)
            {
            }
        }
    }
}
