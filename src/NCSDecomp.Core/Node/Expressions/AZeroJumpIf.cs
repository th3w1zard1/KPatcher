// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AZeroJumpIf : PJumpIf
    {

        private TJz _jz_;

        public AZeroJumpIf()
        {
        }

        public AZeroJumpIf(TJz _jz_)
        {
            SetJz(_jz_);
        }

        public override object Clone()
        {
            TJz clonedJz = _jz_ != null ? (TJz)CloneNode(_jz_) : null;
            return (object)new AZeroJumpIf(clonedJz);
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAZeroJumpIf(this);
        }

        public TJz GetJz()
        {
            return _jz_;
        }

        public void SetJz(TJz node)
        {
            if (_jz_ != null)
            {
                _jz_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _jz_ = node;
        }

        public override string ToString()
        {
            return ToString(_jz_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_jz_ == child)
            {
                _jz_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_jz_ == oldChild)
            {
                SetJz((TJz)newChild);
            }
        }
    }

}
