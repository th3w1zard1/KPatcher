// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ABpCmd : PCmd
    {

        private PBpCommand _bpCommand_;

        public ABpCmd()
        {
        }

        public ABpCmd(PBpCommand _bpCommand_)
        {
            SetBpCommand(_bpCommand_);
        }

        public override object Clone()
        {
            return (object)new ABpCmd((PBpCommand)CloneNode(_bpCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseABpCmd(this);
        }

        public PBpCommand GetBpCommand()
        {
            return _bpCommand_;
        }

        public void SetBpCommand(PBpCommand node)
        {
            if (_bpCommand_ != null)
            {
                _bpCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _bpCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_bpCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_bpCommand_ == child)
            {
                _bpCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_bpCommand_ == oldChild)
            {
                SetBpCommand((PBpCommand)newChild);
            }
        }
    }

}
