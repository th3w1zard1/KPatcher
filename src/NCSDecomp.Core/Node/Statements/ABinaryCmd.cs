// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ABinaryCmd : PCmd
    {

        private PBinaryCommand _binaryCommand_;

        public ABinaryCmd()
        {
        }

        public ABinaryCmd(PBinaryCommand _binaryCommand_)
        {
            SetBinaryCommand(_binaryCommand_);
        }

        public override object Clone()
        {
            return (object)new ABinaryCmd((PBinaryCommand)CloneNode(_binaryCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseABinaryCmd(this);
        }

        public PBinaryCommand GetBinaryCommand()
        {
            return _binaryCommand_;
        }

        public void SetBinaryCommand(PBinaryCommand node)
        {
            if (_binaryCommand_ != null)
            {
                _binaryCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _binaryCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_binaryCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_binaryCommand_ == child)
            {
                _binaryCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_binaryCommand_ == oldChild)
            {
                SetBinaryCommand((PBinaryCommand)newChild);
            }
        }
    }

}
