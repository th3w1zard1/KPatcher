// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AOrLogiiOp : PLogiiOp
    {

        private TLogorii _logorii_;

        public AOrLogiiOp()
        {
        }

        public AOrLogiiOp(TLogorii _logorii_)
        {
            SetLogorii(_logorii_);
        }

        public override object Clone()
        {
            return (object)new AOrLogiiOp((TLogorii)CloneNode(_logorii_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAOrLogiiOp(this);
        }

        public TLogorii GetLogorii()
        {
            return _logorii_;
        }

        public void SetLogorii(TLogorii node)
        {
            if (_logorii_ != null)
            {
                _logorii_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _logorii_ = node;
        }

        public override string ToString()
        {
            return ToString(_logorii_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_logorii_ == child)
            {
                _logorii_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_logorii_ == oldChild)
            {
                SetLogorii((TLogorii)newChild);
            }
        }
    }

}
