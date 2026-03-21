// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AGeqBinaryOp : PBinaryOp
    {

        private TGeq _geq_;

        public AGeqBinaryOp()
        {
        }

        public AGeqBinaryOp(TGeq _geq_)
        {
            SetGeq(_geq_);
        }

        public override object Clone()
        {
            return (object)new AGeqBinaryOp((TGeq)CloneNode(_geq_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAGeqBinaryOp(this);
        }

        public TGeq GetGeq()
        {
            return _geq_;
        }

        public void SetGeq(TGeq node)
        {
            if (_geq_ != null)
            {
                _geq_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _geq_ = node;
        }

        public override string ToString()
        {
            return ToString(_geq_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_geq_ == child)
            {
                _geq_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_geq_ == oldChild)
            {
                SetGeq((TGeq)newChild);
            }
        }
    }

}
