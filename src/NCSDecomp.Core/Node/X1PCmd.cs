// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;

namespace NCSDecomp.Core.Node
{
    public sealed class X1PCmd : XPCmd
    {
        private XPCmd _xPCmd_;
        private PCmd _pCmd_;

        public X1PCmd()
        {
        }

        public X1PCmd(XPCmd xPCmd, PCmd pCmd)
        {
            SetXPCmd(xPCmd);
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

        public XPCmd GetXPCmd()
        {
            return _xPCmd_;
        }

        public void SetXPCmd(XPCmd node)
        {
            if (_xPCmd_ != null)
            {
                _xPCmd_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _xPCmd_ = node;
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
            if (_xPCmd_ == child)
            {
                _xPCmd_ = null;
            }

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
            return ToString(_xPCmd_) + ToString(_pCmd_);
        }
    }
}
