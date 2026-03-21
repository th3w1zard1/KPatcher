// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ACompUnaryOp : PUnaryOp
    {

        private TComp _comp_;

        public ACompUnaryOp()
        {
        }

        public ACompUnaryOp(TComp _comp_)
        {
            SetComp(_comp_);
        }

        public override object Clone()
        {
            return (object)new ACompUnaryOp((TComp)CloneNode(_comp_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseACompUnaryOp(this);
        }

        public TComp GetComp()
        {
            return _comp_;
        }

        public void SetComp(TComp node)
        {
            if (_comp_ != null)
            {
                _comp_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _comp_ = node;
        }

        public override string ToString()
        {
            return ToString(_comp_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_comp_ == child)
            {
                _comp_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_comp_ == oldChild)
            {
                SetComp((TComp)newChild);
            }
        }
    }

}
