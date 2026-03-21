// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ADecibpStackOp : PStackOp
    {

        private TDecibp _decibp_;

        public ADecibpStackOp()
        {
        }

        public ADecibpStackOp(TDecibp _decibp_)
        {
            SetDecibp(_decibp_);
        }

        public override object Clone()
        {
            return (object)new ADecibpStackOp((TDecibp)CloneNode(_decibp_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseADecibpStackOp(this);
        }

        public TDecibp GetDecibp()
        {
            return _decibp_;
        }

        public void SetDecibp(TDecibp node)
        {
            if (_decibp_ != null)
            {
                _decibp_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _decibp_ = node;
        }

        public override string ToString()
        {
            return ToString(_decibp_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_decibp_ == child)
            {
                _decibp_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_decibp_ == oldChild)
            {
                SetDecibp((TDecibp)newChild);
            }
        }
    }

}
