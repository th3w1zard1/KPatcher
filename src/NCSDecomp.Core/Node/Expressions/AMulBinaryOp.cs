// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AMulBinaryOp : PBinaryOp
    {

        private TMul _mul_;

        public AMulBinaryOp()
        {
        }

        public AMulBinaryOp(TMul _mul_)
        {
            SetMul(_mul_);
        }

        public override object Clone()
        {
            return (object)new AMulBinaryOp((TMul)CloneNode(_mul_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAMulBinaryOp(this);
        }

        public TMul GetMul()
        {
            return _mul_;
        }

        public void SetMul(TMul node)
        {
            if (_mul_ != null)
            {
                _mul_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _mul_ = node;
        }

        public override string ToString()
        {
            return ToString(_mul_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_mul_ == child)
            {
                _mul_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_mul_ == oldChild)
            {
                SetMul((TMul)newChild);
            }
        }
    }

}
