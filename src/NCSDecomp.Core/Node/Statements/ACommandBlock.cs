// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    /// <summary>
    /// SableCC node that holds an ordered list of commands within a subroutine.
    /// </summary>
    public sealed class ACommandBlock : PCommandBlock
    {
        private readonly TypedLinkedList<PCmd> _cmd_;

        public ACommandBlock()
        {
            _cmd_ = new TypedLinkedList<PCmd>(new CmdCast(this));
        }

        public ACommandBlock(IEnumerable<PCmd> cmd)
            : this()
        {
            _cmd_.Clear();
            _cmd_.AddAll(cmd);
        }

        public ACommandBlock(XPCmd cmd)
            : this()
        {
            if (cmd != null)
            {
                while (cmd is X1PCmd x1)
                {
                    _cmd_.AddFirst(x1.GetPCmd());
                    cmd = x1.GetXPCmd();
                }

                _cmd_.AddFirst(((X2PCmd)cmd).GetPCmd());
            }
        }

        public override object Clone()
        {
            return (object)new ACommandBlock(CloneTypedList(_cmd_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseACommandBlock(this);
        }

        public TypedLinkedList<PCmd> GetCmd()
        {
            return _cmd_;
        }

        public void SetCmd(IEnumerable<PCmd> list)
        {
            _cmd_.Clear();
            _cmd_.AddAll(list);
        }

        public override string ToString()
        {
            return ToStringEnumerable(_cmd_);
        }

        internal override void RemoveChild(Node child)
        {
            _cmd_.Remove((PCmd)child);
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            _cmd_.ReplaceChild((PCmd)oldChild, (PCmd)newChild);
        }

        private sealed class CmdCast : ICast<PCmd>
        {
            private readonly ACommandBlock _outer;

            public CmdCast(ACommandBlock outer)
            {
                _outer = outer;
            }

            public PCmd CastObject(object o)
            {
                if (!(o is PCmd node))
                {
                    throw new System.InvalidCastException("Expected PCmd but got: " + (o != null ? o.GetType().FullName : "null"));
                }

                if (node.Parent() != null && node.Parent() != _outer)
                {
                    node.Parent().RemoveChild(node);
                }

                if (node.Parent() == null || node.Parent() != _outer)
                {
                    node.SetParent(_outer);
                }

                return node;
            }
        }
    }
}
