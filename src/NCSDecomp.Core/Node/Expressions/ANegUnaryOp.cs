// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ANegUnaryOp : PUnaryOp
    {

        private TNeg _neg_;

        public ANegUnaryOp()
        {
        }

        public ANegUnaryOp(TNeg _neg_)
        {
            SetNeg(_neg_);
        }

        public override object Clone()
        {
            return (object)new ANegUnaryOp((TNeg)CloneNode(_neg_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseANegUnaryOp(this);
        }

        public TNeg GetNeg()
        {
            return _neg_;
        }

        public void SetNeg(TNeg node)
        {
            if (_neg_ != null)
            {
                _neg_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _neg_ = node;
        }

        public override string ToString()
        {
            return ToString(_neg_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_neg_ == child)
            {
                _neg_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_neg_ == oldChild)
            {
                SetNeg((TNeg)newChild);
            }
        }
    }

}
