// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AReturnCmd : PCmd
    {

        private PReturn _return_;

        public AReturnCmd()
        {
        }

        public AReturnCmd(PReturn _return_)
        {
            SetReturn(_return_);
        }

        public override object Clone()
        {
            return (object)new AReturnCmd((PReturn)CloneNode(_return_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAReturnCmd(this);
        }

        public PReturn GetReturn()
        {
            return _return_;
        }

        public void SetReturn(PReturn node)
        {
            if (_return_ != null)
            {
                _return_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _return_ = node;
        }

        public override string ToString()
        {
            return ToString(_return_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_return_ == child)
            {
                _return_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_return_ == oldChild)
            {
                SetReturn((PReturn)newChild);
            }
        }
    }

}
