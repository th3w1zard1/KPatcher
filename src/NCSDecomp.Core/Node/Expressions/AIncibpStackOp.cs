// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AIncibpStackOp : PStackOp
    {

        private TIncibp _incibp_;

        public AIncibpStackOp()
        {
        }

        public AIncibpStackOp(TIncibp _incibp_)
        {
            SetIncibp(_incibp_);
        }

        public override object Clone()
        {
            return (object)new AIncibpStackOp((TIncibp)CloneNode(_incibp_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAIncibpStackOp(this);
        }

        public TIncibp GetIncibp()
        {
            return _incibp_;
        }

        public void SetIncibp(TIncibp node)
        {
            if (_incibp_ != null)
            {
                _incibp_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _incibp_ = node;
        }

        public override string ToString()
        {
            return ToString(_incibp_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_incibp_ == child)
            {
                _incibp_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_incibp_ == oldChild)
            {
                SetIncibp((TIncibp)newChild);
            }
        }
    }

}
