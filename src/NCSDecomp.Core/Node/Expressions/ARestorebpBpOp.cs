// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ARestorebpBpOp : PBpOp
    {

        private TRestorebp _restorebp_;

        public ARestorebpBpOp()
        {
        }

        public ARestorebpBpOp(TRestorebp _restorebp_)
        {
            SetRestorebp(_restorebp_);
        }

        public override object Clone()
        {
            return (object)new ARestorebpBpOp((TRestorebp)CloneNode(_restorebp_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseARestorebpBpOp(this);
        }

        public TRestorebp GetRestorebp()
        {
            return _restorebp_;
        }

        public void SetRestorebp(TRestorebp node)
        {
            if (_restorebp_ != null)
            {
                _restorebp_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _restorebp_ = node;
        }

        public override string ToString()
        {
            return ToString(_restorebp_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_restorebp_ == child)
            {
                _restorebp_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_restorebp_ == oldChild)
            {
                SetRestorebp((TRestorebp)newChild);
            }
        }
    }

}
