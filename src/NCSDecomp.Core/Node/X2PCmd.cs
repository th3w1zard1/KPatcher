// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;

namespace NCSDecomp.Core.Node
{
    public sealed class X2PCmd : XPCmd
    {
        private PCmd _pCmd_;

        public X2PCmd()
        {
        }

        public X2PCmd(PCmd pCmd)
        {
            SetPCmd(pCmd);
        }

        public override object Clone()
        {
            throw new NotSupportedException("Unsupported Operation");
        }

        public override void Apply(Switch sw)
        {
            throw new NotSupportedException("Switch not supported.");
        }

        public PCmd GetPCmd()
        {
            return _pCmd_;
        }

        public void SetPCmd(PCmd node)
        {
            if (_pCmd_ != null)
            {
                _pCmd_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _pCmd_ = node;
        }

        internal override void RemoveChild(Node child)
        {
            if (_pCmd_ == child)
            {
                _pCmd_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
        }

        public override string ToString()
        {
            return ToString(_pCmd_);
        }
    }
}
