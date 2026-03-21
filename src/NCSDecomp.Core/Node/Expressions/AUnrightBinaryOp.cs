// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AUnrightBinaryOp : PBinaryOp
    {

        private TUnright _unright_;

        public AUnrightBinaryOp()
        {
        }

        public AUnrightBinaryOp(TUnright _unright_)
        {
            SetUnright(_unright_);
        }

        public override object Clone()
        {
            return (object)new AUnrightBinaryOp((TUnright)CloneNode(_unright_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAUnrightBinaryOp(this);
        }

        public TUnright GetUnright()
        {
            return _unright_;
        }

        public void SetUnright(TUnright node)
        {
            if (_unright_ != null)
            {
                _unright_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _unright_ = node;
        }

        public override string ToString()
        {
            return ToString(_unright_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_unright_ == child)
            {
                _unright_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_unright_ == oldChild)
            {
                SetUnright((TUnright)newChild);
            }
        }
    }

}
