// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AGtBinaryOp : PBinaryOp
    {

        private TGt _gt_;

        public AGtBinaryOp()
        {
        }

        public AGtBinaryOp(TGt _gt_)
        {
            SetGt(_gt_);
        }

        public override object Clone()
        {
            return (object)new AGtBinaryOp((TGt)CloneNode(_gt_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAGtBinaryOp(this);
        }

        public TGt GetGt()
        {
            return _gt_;
        }

        public void SetGt(TGt node)
        {
            if (_gt_ != null)
            {
                _gt_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _gt_ = node;
        }

        public override string ToString()
        {
            return ToString(_gt_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_gt_ == child)
            {
                _gt_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_gt_ == oldChild)
            {
                SetGt((TGt)newChild);
            }
        }
    }

}
