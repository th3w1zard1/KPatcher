// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ADecispStackOp : PStackOp
    {

        private TDecisp _decisp_;

        public ADecispStackOp()
        {
        }

        public ADecispStackOp(TDecisp _decisp_)
        {
            SetDecisp(_decisp_);
        }

        public override object Clone()
        {
            return (object)new ADecispStackOp((TDecisp)CloneNode(_decisp_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseADecispStackOp(this);
        }

        public TDecisp GetDecisp()
        {
            return _decisp_;
        }

        public void SetDecisp(TDecisp node)
        {
            if (_decisp_ != null)
            {
                _decisp_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _decisp_ = node;
        }

        public override string ToString()
        {
            return ToString(_decisp_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_decisp_ == child)
            {
                _decisp_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_decisp_ == oldChild)
            {
                SetDecisp((TDecisp)newChild);
            }
        }
    }

}
