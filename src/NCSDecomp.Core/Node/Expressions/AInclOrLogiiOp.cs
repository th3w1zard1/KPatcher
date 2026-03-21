// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AInclOrLogiiOp : PLogiiOp
    {

        private TIncorii _incorii_;

        public AInclOrLogiiOp()
        {
        }

        public AInclOrLogiiOp(TIncorii _incorii_)
        {
            SetIncorii(_incorii_);
        }

        public override object Clone()
        {
            return (object)new AInclOrLogiiOp((TIncorii)CloneNode(_incorii_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAInclOrLogiiOp(this);
        }

        public TIncorii GetIncorii()
        {
            return _incorii_;
        }

        public void SetIncorii(TIncorii node)
        {
            if (_incorii_ != null)
            {
                _incorii_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _incorii_ = node;
        }

        public override string ToString()
        {
            return ToString(_incorii_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_incorii_ == child)
            {
                _incorii_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_incorii_ == oldChild)
            {
                SetIncorii((TIncorii)newChild);
            }
        }
    }

}
