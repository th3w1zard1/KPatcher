// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ANonzeroJumpIf : PJumpIf
    {

        private TJnz _jnz_;

        public ANonzeroJumpIf()
        {
        }

        public ANonzeroJumpIf(TJnz _jnz_)
        {
            SetJnz(_jnz_);
        }

        public override object Clone()
        {
            return (object)new ANonzeroJumpIf((TJnz)CloneNode(_jnz_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseANonzeroJumpIf(this);
        }

        public TJnz GetJnz()
        {
            return _jnz_;
        }

        public void SetJnz(TJnz node)
        {
            if (_jnz_ != null)
            {
                _jnz_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _jnz_ = node;
        }

        public override string ToString()
        {
            return ToString(_jnz_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_jnz_ == child)
            {
                _jnz_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_jnz_ == oldChild)
            {
                SetJnz((TJnz)newChild);
            }
        }
    }

}
