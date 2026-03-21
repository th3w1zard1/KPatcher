// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ANequalBinaryOp : PBinaryOp
    {

        private TNequal _nequal_;

        public ANequalBinaryOp()
        {
        }

        public ANequalBinaryOp(TNequal _nequal_)
        {
            SetNequal(_nequal_);
        }

        public override object Clone()
        {
            return (object)new ANequalBinaryOp((TNequal)CloneNode(_nequal_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseANequalBinaryOp(this);
        }

        public TNequal GetNequal()
        {
            return _nequal_;
        }

        public void SetNequal(TNequal node)
        {
            if (_nequal_ != null)
            {
                _nequal_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _nequal_ = node;
        }

        public override string ToString()
        {
            return ToString(_nequal_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_nequal_ == child)
            {
                _nequal_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_nequal_ == oldChild)
            {
                SetNequal((TNequal)newChild);
            }
        }
    }

}
