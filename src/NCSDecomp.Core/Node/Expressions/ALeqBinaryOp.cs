// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ALeqBinaryOp : PBinaryOp
    {

        private TLeq _leq_;

        public ALeqBinaryOp()
        {
        }

        public ALeqBinaryOp(TLeq _leq_)
        {
            SetLeq(_leq_);
        }

        public override object Clone()
        {
            return (object)new ALeqBinaryOp((TLeq)CloneNode(_leq_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseALeqBinaryOp(this);
        }

        public TLeq GetLeq()
        {
            return _leq_;
        }

        public void SetLeq(TLeq node)
        {
            if (_leq_ != null)
            {
                _leq_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _leq_ = node;
        }

        public override string ToString()
        {
            return ToString(_leq_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_leq_ == child)
            {
                _leq_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_leq_ == oldChild)
            {
                SetLeq((TLeq)newChild);
            }
        }
    }

}
