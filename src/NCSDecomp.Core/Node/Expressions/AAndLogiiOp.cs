// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AAndLogiiOp : PLogiiOp
    {

        private TLogandii _logandii_;

        public AAndLogiiOp()
        {
        }

        public AAndLogiiOp(TLogandii _logandii_)
        {
            SetLogandii(_logandii_);
        }

        public override object Clone()
        {
            return (object)new AAndLogiiOp((TLogandii)CloneNode(_logandii_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAAndLogiiOp(this);
        }

        public TLogandii GetLogandii()
        {
            return _logandii_;
        }

        public void SetLogandii(TLogandii node)
        {
            if (_logandii_ != null)
            {
                _logandii_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _logandii_ = node;
        }

        public override string ToString()
        {
            return ToString(_logandii_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_logandii_ == child)
            {
                _logandii_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_logandii_ == oldChild)
            {
                SetLogandii((TLogandii)newChild);
            }
        }
    }

}
