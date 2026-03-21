// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS CleanupPass.java.

using System.Collections.Generic;
using NCSDecomp.Core.Stack;
using NCSDecomp.Core.Utils;
using Sn = NCSDecomp.Core.ScriptNode;

namespace NCSDecomp.Core.ScriptUtils
{
    /// <summary>
    /// Normalizes generated script AST (flatten single block, merge decls, wrap expressions).
    /// </summary>
    public sealed class CleanupPass
    {
        private Sn.ASub root;
        private NodeAnalysisData nodedata;
        private SubroutineAnalysisData subdata;
        private SubScriptState state;

        public CleanupPass(Sn.ASub root, NodeAnalysisData nodedata, SubroutineAnalysisData subdata, SubScriptState state)
        {
            this.root = root;
            this.nodedata = nodedata;
            this.subdata = subdata;
            this.state = state;
        }

        public void Apply()
        {
            CheckSubCodeBlock();
            ApplyInner(root);
        }

        public void Done()
        {
            root = null;
            nodedata = null;
            subdata = null;
            state = null;
        }

        private void CheckSubCodeBlock()
        {
            if (root.Size() == 1 && root.GetLastChild() is Sn.ACodeBlock block)
            {
                root.RemoveLastChild();
                List<Sn.ScriptNode> ch = block.RemoveChildren();
                root.AddChildren(ch);
            }
        }

        private static LinkedListNode<Sn.ScriptNode> NodeAt(LinkedList<Sn.ScriptNode> list, int index)
        {
            LinkedListNode<Sn.ScriptNode> n = list.First;
            for (int j = 0; j < index && n != null; j++)
            {
                n = n.Next;
            }

            return n;
        }

        private void ApplyInner(Sn.ScriptRootNode rootnode)
        {
            LinkedList<Sn.ScriptNode> children = rootnode.GetChildren();
            int i = 0;
            while (i < children.Count)
            {
                Sn.ScriptNode node1 = NodeAt(children, i).Value;

                if (node1 is Sn.AVarDecl decl && decl.Exp() == null && i + 1 < children.Count)
                {
                    Sn.ScriptNode maybeAssign = NodeAt(children, i + 1).Value;
                    if (maybeAssign is Sn.AExpressionStatement es && es.Exp() is Sn.AModifyExp modexp)
                    {
                        if (ReferenceEquals(modexp.VarRef().Var(), decl.Var()))
                        {
                            decl.InitializeExp(modexp.Expression());
                            children.Remove(maybeAssign);
                            continue;
                        }
                    }
                }

                if (node1 is Sn.AVarDecl vdecl)
                {
                    Variable var = vdecl.Var();
                    if (var != null && var.IsStruct())
                    {
                        VarStruct structVar = var.Varstruct();
                        var structdec = new Sn.AVarDecl(structVar);
                        int idx = i + 1;
                        while (idx < children.Count)
                        {
                            Sn.ScriptNode cursor = NodeAt(children, idx).Value;
                            if (!(cursor is Sn.AVarDecl vd2) || vd2.Var() == null || !vd2.Var().IsStruct() ||
                                !ReferenceEquals(vd2.Var().Varstruct(), structVar))
                            {
                                break;
                            }

                            children.Remove(cursor);
                            cursor.Parent(null);
                        }

                        rootnode.ReplaceChild(vdecl, structdec);
                        node1 = structdec;
                    }
                }

                if (IsDanglingExpression(node1))
                {
                    var expstm = new Sn.AExpressionStatement((Sn.IAExpression)node1);
                    expstm.Parent(rootnode);
                    rootnode.ReplaceChild(node1, expstm);
                }

                if (node1 is Sn.ScriptRootNode srn)
                {
                    ApplyInner(srn);
                }

                if (node1 is Sn.ASwitch sw)
                {
                    Sn.ASwitchCase acase = null;
                    while ((acase = sw.GetNextCase(acase)) != null)
                    {
                        ApplyInner(acase);
                    }
                }

                i++;
            }
        }

        private static bool IsDanglingExpression(Sn.ScriptNode node)
        {
            return node is Sn.IAExpression && !(node is Sn.ScriptRootNode);
        }
    }
}
