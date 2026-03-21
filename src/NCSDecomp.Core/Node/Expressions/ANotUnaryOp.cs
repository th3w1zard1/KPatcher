// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ANotUnaryOp : PUnaryOp
    {

        private TNot _not_;

        public ANotUnaryOp()
        {
        }

        public ANotUnaryOp(TNot _not_)
        {
            SetNot(_not_);
        }

        public override object Clone()
        {
            return (object)new ANotUnaryOp((TNot)CloneNode(_not_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseANotUnaryOp(this);
        }

        public TNot GetNot()
        {
            return _not_;
        }

        public void SetNot(TNot node)
        {
            if (_not_ != null)
            {
                _not_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _not_ = node;
        }

        public override string ToString()
        {
            return ToString(_not_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_not_ == child)
            {
                _not_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_not_ == oldChild)
            {
                SetNot((TNot)newChild);
            }
        }
    }

}
