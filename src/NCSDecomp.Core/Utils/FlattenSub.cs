// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS FlattenSub.java.

using System.Collections.Generic;
using NCSDecomp.Core.Analysis;
using NCSDecomp.Core.Node;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Expands action-jump constructs into linear command sequences inside a subroutine.
    /// </summary>
    public sealed class FlattenSub : PrunedDepthFirstAdapter
    {
        private ASubroutine sub;
        private List<PCmd> commands;
        private int i;
        private NodeAnalysisData nodedata;

        public FlattenSub(ASubroutine sub, NodeAnalysisData nodedata)
        {
            SetSub(sub);
            this.nodedata = nodedata;
        }

        public void Done()
        {
            sub = null;
            commands = null;
            nodedata = null;
        }

        public void SetSub(ASubroutine sub)
        {
            this.sub = sub;
        }

        public override void CaseACommandBlock(ACommandBlock node)
        {
            commands = node.GetCmd().ToList();
            i = 0;
            while (i < commands.Count)
            {
                commands[i].Apply(this);
                i++;
            }

            node.SetCmd(commands);
            commands = null;
        }

        public override void CaseAActionJumpCmd(AActionJumpCmd node)
        {
            var sscommand = (AStoreStateCommand)node.GetStoreStateCommand();
            var jmpcommand = (AJumpCommand)node.GetJumpCommand();
            var cmdblock = (ACommandBlock)node.GetCommandBlock();
            var rtn = (AReturn)node.GetReturn();
            var sscmd = new AStoreStateCmd(sscommand);
            var jmpcmd = new AJumpCmd(jmpcommand);
            var rtncmd = new AReturnCmd(rtn);
            nodedata.SetPos(sscmd, nodedata.GetPos(sscommand));
            nodedata.SetPos(jmpcmd, nodedata.GetPos(jmpcommand));
            nodedata.SetPos(rtncmd, nodedata.GetPos(rtn));
            // Replace AActionJumpCmd at i with STORESTATE; JMP; ...body...; RETURN (DeNCS FlattenSub.java)
            int j = i;
            commands[j++] = sscmd;
            commands.Insert(j, jmpcmd);
            j++;
            List<PCmd> subcmds = cmdblock.GetCmd().ToList();
            while (subcmds.Count > 0)
            {
                commands.Insert(j, subcmds[0]);
                subcmds.RemoveAt(0);
                j++;
            }

            commands.Insert(j, rtncmd);
        }

        public override void CaseAAddVarCmd(AAddVarCmd node) { }

        public override void CaseAConstCmd(AConstCmd node) { }

        public override void CaseACopydownspCmd(ACopydownspCmd node) { }

        public override void CaseACopytopspCmd(ACopytopspCmd node) { }

        public override void CaseACopydownbpCmd(ACopydownbpCmd node) { }

        public override void CaseACopytopbpCmd(ACopytopbpCmd node) { }

        public override void CaseAMovespCmd(AMovespCmd node) { }

        public override void CaseALogiiCmd(ALogiiCmd node) { }

        public override void CaseAUnaryCmd(AUnaryCmd node) { }

        public override void CaseABinaryCmd(ABinaryCmd node) { }

        public override void CaseADestructCmd(ADestructCmd node) { }

        public override void CaseABpCmd(ABpCmd node) { }

        public override void CaseAActionCmd(AActionCmd node) { }

        public override void CaseAStackOpCmd(AStackOpCmd node) { }
    }
}
