// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;

namespace NCSDecomp.Core.Node
{
    public sealed class X2PSubroutine : XPSubroutine
    {
        private PSubroutine _pSubroutine_;

        public X2PSubroutine()
        {
        }

        public X2PSubroutine(PSubroutine pSubroutine)
        {
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
            return ToString(_pSubroutine_);
        }
    }
}
