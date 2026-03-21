// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ASavebpBpOp : PBpOp
    {

        private TSavebp _savebp_;

        public ASavebpBpOp()
        {
        }

        public ASavebpBpOp(TSavebp _savebp_)
        {
            SetSavebp(_savebp_);
        }

        public override object Clone()
        {
            return (object)new ASavebpBpOp((TSavebp)CloneNode(_savebp_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseASavebpBpOp(this);
        }

        public TSavebp GetSavebp()
        {
            return _savebp_;
        }

        public void SetSavebp(TSavebp node)
        {
            if (_savebp_ != null)
            {
                _savebp_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _savebp_ = node;
        }

        public override string ToString()
        {
            return ToString(_savebp_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_savebp_ == child)
            {
                _savebp_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_savebp_ == oldChild)
            {
                SetSavebp((TSavebp)newChild);
            }
        }
    }

}
