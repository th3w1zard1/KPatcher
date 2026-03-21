// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AExclOrLogiiOp : PLogiiOp
    {

        private TExcorii _excorii_;

        public AExclOrLogiiOp()
        {
        }

        public AExclOrLogiiOp(TExcorii _excorii_)
        {
            SetExcorii(_excorii_);
        }

        public override object Clone()
        {
            return (object)new AExclOrLogiiOp((TExcorii)CloneNode(_excorii_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAExclOrLogiiOp(this);
        }

        public TExcorii GetExcorii()
        {
            return _excorii_;
        }

        public void SetExcorii(TExcorii node)
        {
            if (_excorii_ != null)
            {
                _excorii_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _excorii_ = node;
        }

        public override string ToString()
        {
            return ToString(_excorii_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_excorii_ == child)
            {
                _excorii_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_excorii_ == oldChild)
            {
                SetExcorii((TExcorii)newChild);
            }
        }
    }

}
