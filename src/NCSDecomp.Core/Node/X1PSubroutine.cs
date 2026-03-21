// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;

namespace NCSDecomp.Core.Node
{
    public sealed class X1PSubroutine : XPSubroutine
    {
        private XPSubroutine _xPSubroutine_;
        private PSubroutine _pSubroutine_;

        public X1PSubroutine()
        {
        }

        public X1PSubroutine(XPSubroutine xPSubroutine, PSubroutine pSubroutine)
        {
            SetXPSubroutine(xPSubroutine);
            SetPSubroutine(pSubroutine);
        }

        public override object Clone()
        {
            throw new NotSupportedException("Unsupported Operation");
        }

        public override void Apply(Switch sw)
        {
            throw new NotSupportedException("Switch not supported.");
        }

        public XPSubroutine GetXPSubroutine()
        {
            return _xPSubroutine_;
        }

        public void SetXPSubroutine(XPSubroutine node)
        {
            if (_xPSubroutine_ != null)
            {
                _xPSubroutine_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _xPSubroutine_ = node;
        }

        public PSubroutine GetPSubroutine()
        {
            return _pSubroutine_;
        }

        public void SetPSubroutine(PSubroutine node)
        {
            if (_pSubroutine_ != null)
            {
                _pSubroutine_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _pSubroutine_ = node;
        }

        internal override void RemoveChild(Node child)
        {
            if (_xPSubroutine_ == child)
            {
                _xPSubroutine_ = null;
            }

            if (_pSubroutine_ == child)
            {
                _pSubroutine_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
        }

        public override string ToString()
        {
            return ToString(_xPSubroutine_) + ToString(_pSubroutine_);
        }
    }
}
