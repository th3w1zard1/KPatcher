// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ABitAndLogiiOp : PLogiiOp
    {

        private TBoolandii _boolandii_;

        public ABitAndLogiiOp()
        {
        }

        public ABitAndLogiiOp(TBoolandii _boolandii_)
        {
            SetBoolandii(_boolandii_);
        }

        public override object Clone()
        {
            return (object)new ABitAndLogiiOp((TBoolandii)CloneNode(_boolandii_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseABitAndLogiiOp(this);
        }

        public TBoolandii GetBoolandii()
        {
            return _boolandii_;
        }

        public void SetBoolandii(TBoolandii node)
        {
            if (_boolandii_ != null)
            {
                _boolandii_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _boolandii_ = node;
        }

        public override string ToString()
        {
            return ToString(_boolandii_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_boolandii_ == child)
            {
                _boolandii_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_boolandii_ == oldChild)
            {
                SetBoolandii((TBoolandii)newChild);
            }
        }
    }

}
