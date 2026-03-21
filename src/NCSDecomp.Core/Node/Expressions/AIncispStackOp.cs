// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AIncispStackOp : PStackOp
    {

        private TIncisp _incisp_;

        public AIncispStackOp()
        {
        }

        public AIncispStackOp(TIncisp _incisp_)
        {
            SetIncisp(_incisp_);
        }

        public override object Clone()
        {
            return (object)new AIncispStackOp((TIncisp)CloneNode(_incisp_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAIncispStackOp(this);
        }

        public TIncisp GetIncisp()
        {
            return _incisp_;
        }

        public void SetIncisp(TIncisp node)
        {
            if (_incisp_ != null)
            {
                _incisp_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _incisp_ = node;
        }

        public override string ToString()
        {
            return ToString(_incisp_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_incisp_ == child)
            {
                _incisp_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_incisp_ == oldChild)
            {
                SetIncisp((TIncisp)newChild);
            }
        }
    }

}
