// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ALtBinaryOp : PBinaryOp
    {

        private TLt _lt_;

        public ALtBinaryOp()
        {
        }

        public ALtBinaryOp(TLt _lt_)
        {
            SetLt(_lt_);
        }

        public override object Clone()
        {
            return (object)new ALtBinaryOp((TLt)CloneNode(_lt_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseALtBinaryOp(this);
        }

        public TLt GetLt()
        {
            return _lt_;
        }

        public void SetLt(TLt node)
        {
            if (_lt_ != null)
            {
                _lt_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _lt_ = node;
        }

        public override string ToString()
        {
            return ToString(_lt_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_lt_ == child)
            {
                _lt_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_lt_ == oldChild)
            {
                SetLt((TLt)newChild);
            }
        }
    }

}
