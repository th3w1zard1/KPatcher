// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AIntConstant : PConstant
    {

        private TIntegerConstant _integerConstant_;

        public AIntConstant()
        {
        }

        public AIntConstant(TIntegerConstant _integerConstant_)
        {
            SetIntegerConstant(_integerConstant_);
        }

        public override object Clone()
        {
            return (object)new AIntConstant((TIntegerConstant)CloneNode(_integerConstant_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAIntConstant(this);
        }

        public TIntegerConstant GetIntegerConstant()
        {
            return _integerConstant_;
        }

        public void SetIntegerConstant(TIntegerConstant node)
        {
            if (_integerConstant_ != null)
            {
                _integerConstant_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _integerConstant_ = node;
        }

        public override string ToString()
        {
            return ToString(_integerConstant_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_integerConstant_ == child)
            {
                _integerConstant_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_integerConstant_ == oldChild)
            {
                SetIntegerConstant((TIntegerConstant)newChild);
            }
        }
    }

}
