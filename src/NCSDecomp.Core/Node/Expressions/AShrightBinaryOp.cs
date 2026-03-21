// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AShrightBinaryOp : PBinaryOp
    {

        private TShright _shright_;

        public AShrightBinaryOp()
        {
        }

        public AShrightBinaryOp(TShright _shright_)
        {
            SetShright(_shright_);
        }

        public override object Clone()
        {
            return (object)new AShrightBinaryOp((TShright)CloneNode(_shright_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAShrightBinaryOp(this);
        }

        public TShright GetShright()
        {
            return _shright_;
        }

        public void SetShright(TShright node)
        {
            if (_shright_ != null)
            {
                _shright_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _shright_ = node;
        }

        public override string ToString()
        {
            return ToString(_shright_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_shright_ == child)
            {
                _shright_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_shright_ == oldChild)
            {
                SetShright((TShright)newChild);
            }
        }
    }

}
