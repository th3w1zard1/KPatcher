// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AStringConstant : PConstant
    {

        private TStringLiteral _stringLiteral_;

        public AStringConstant()
        {
        }

        public AStringConstant(TStringLiteral _stringLiteral_)
        {
            SetStringLiteral(_stringLiteral_);
        }

        public override object Clone()
        {
            TStringLiteral clonedStringLiteral = _stringLiteral_ != null ? (TStringLiteral)CloneNode(_stringLiteral_) : null;
            return (object)new AStringConstant(clonedStringLiteral);
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAStringConstant(this);
        }

        public TStringLiteral GetStringLiteral()
        {
            return _stringLiteral_;
        }

        public void SetStringLiteral(TStringLiteral node)
        {
            if (_stringLiteral_ != null)
            {
                _stringLiteral_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _stringLiteral_ = node;
        }

        public override string ToString()
        {
            return ToString(_stringLiteral_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_stringLiteral_ == child)
            {
                _stringLiteral_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_stringLiteral_ == oldChild)
            {
                SetStringLiteral((TStringLiteral)newChild);
            }
        }
    }

}
