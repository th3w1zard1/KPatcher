// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AShleftBinaryOp : PBinaryOp
    {

        private TShleft _shleft_;

        public AShleftBinaryOp()
        {
        }

        public AShleftBinaryOp(TShleft _shleft_)
        {
            SetShleft(_shleft_);
        }

        public override object Clone()
        {
            return (object)new AShleftBinaryOp((TShleft)CloneNode(_shleft_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAShleftBinaryOp(this);
        }

        public TShleft GetShleft()
        {
            return _shleft_;
        }

        public void SetShleft(TShleft node)
        {
            if (_shleft_ != null)
            {
                _shleft_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _shleft_ = node;
        }

        public override string ToString()
        {
            return ToString(_shleft_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_shleft_ == child)
            {
                _shleft_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_shleft_ == oldChild)
            {
                SetShleft((TShleft)newChild);
            }
        }
    }

}
