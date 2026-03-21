// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AEqualBinaryOp : PBinaryOp
    {

        private TEqual _equal_;

        public AEqualBinaryOp()
        {
        }

        public AEqualBinaryOp(TEqual _equal_)
        {
            SetEqual(_equal_);
        }

        public override object Clone()
        {
            return (object)new AEqualBinaryOp((TEqual)CloneNode(_equal_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAEqualBinaryOp(this);
        }

        public TEqual GetEqual()
        {
            return _equal_;
        }

        public void SetEqual(TEqual node)
        {
            if (_equal_ != null)
            {
                _equal_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _equal_ = node;
        }

        public override string ToString()
        {
            return ToString(_equal_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_equal_ == child)
            {
                _equal_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_equal_ == oldChild)
            {
                SetEqual((TEqual)newChild);
            }
        }
    }

}
