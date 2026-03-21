// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AAddBinaryOp : PBinaryOp
    {

        private TAdd _add_;

        public AAddBinaryOp()
        {
        }

        public AAddBinaryOp(TAdd _add_)
        {
            SetAdd(_add_);
        }

        public override object Clone()
        {
            return (object)new AAddBinaryOp((TAdd)CloneNode(_add_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAAddBinaryOp(this);
        }

        public TAdd GetAdd()
        {
            return _add_;
        }

        public void SetAdd(TAdd node)
        {
            if (_add_ != null)
            {
                _add_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _add_ = node;
        }

        public override string ToString()
        {
            return ToString(_add_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_add_ == child)
            {
                _add_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_add_ == oldChild)
            {
                SetAdd((TAdd)newChild);
            }
        }
    }

}
