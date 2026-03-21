// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AFloatConstant : PConstant
    {

        private TFloatConstant _floatConstant_;

        public AFloatConstant()
        {
        }

        public AFloatConstant(TFloatConstant _floatConstant_)
        {
            SetFloatConstant(_floatConstant_);
        }

        public override object Clone()
        {
            return (object)new AFloatConstant((TFloatConstant)CloneNode(_floatConstant_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAFloatConstant(this);
        }

        public TFloatConstant GetFloatConstant()
        {
            return _floatConstant_;
        }

        public void SetFloatConstant(TFloatConstant node)
        {
            if (_floatConstant_ != null)
            {
                _floatConstant_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _floatConstant_ = node;
        }

        public override string ToString()
        {
            return ToString(_floatConstant_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_floatConstant_ == child)
            {
                _floatConstant_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_floatConstant_ == oldChild)
            {
                SetFloatConstant((TFloatConstant)newChild);
            }
        }
    }

}
