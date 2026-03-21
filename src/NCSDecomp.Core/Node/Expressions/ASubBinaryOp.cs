// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ASubBinaryOp : PBinaryOp
    {

        private TSub _sub_;

        public ASubBinaryOp()
        {
        }

        public ASubBinaryOp(TSub _sub_)
        {
            SetSub(_sub_);
        }

        public override object Clone()
        {
            TSub clonedSub = _sub_ != null ? (TSub)CloneNode(_sub_) : null;
            return (object)new ASubBinaryOp(clonedSub);
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseASubBinaryOp(this);
        }

        public TSub GetSub()
        {
            return _sub_;
        }

        public void SetSub(TSub node)
        {
            if (_sub_ != null)
            {
                _sub_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _sub_ = node;
        }

        public override string ToString()
        {
            return ToString(_sub_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_sub_ == child)
            {
                _sub_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_sub_ == oldChild)
            {
                SetSub((TSub)newChild);
            }
        }
    }

}
