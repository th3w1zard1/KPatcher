// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ADivBinaryOp : PBinaryOp
    {

        private TDiv _div_;

        public ADivBinaryOp()
        {
        }

        public ADivBinaryOp(TDiv _div_)
        {
            SetDiv(_div_);
        }

        public override object Clone()
        {
            return (object)new ADivBinaryOp((TDiv)CloneNode(_div_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseADivBinaryOp(this);
        }

        public TDiv GetDiv()
        {
            return _div_;
        }

        public void SetDiv(TDiv node)
        {
            if (_div_ != null)
            {
                _div_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _div_ = node;
        }

        public override string ToString()
        {
            return ToString(_div_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_div_ == child)
            {
                _div_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_div_ == oldChild)
            {
                SetDiv((TDiv)newChild);
            }
        }
    }

}
