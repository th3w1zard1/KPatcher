// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AModBinaryOp : PBinaryOp
    {

        private TMod _mod_;

        public AModBinaryOp()
        {
        }

        public AModBinaryOp(TMod _mod_)
        {
            SetMod(_mod_);
        }

        public override object Clone()
        {
            return (object)new AModBinaryOp((TMod)CloneNode(_mod_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAModBinaryOp(this);
        }

        public TMod GetMod()
        {
            return _mod_;
        }

        public void SetMod(TMod node)
        {
            if (_mod_ != null)
            {
                _mod_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _mod_ = node;
        }

        public override string ToString()
        {
            return ToString(_mod_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_mod_ == child)
            {
                _mod_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_mod_ == oldChild)
            {
                SetMod((TMod)newChild);
            }
        }
    }

}
