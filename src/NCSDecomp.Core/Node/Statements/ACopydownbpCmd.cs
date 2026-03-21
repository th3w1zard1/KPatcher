// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ACopydownbpCmd : PCmd
    {

        private PCopyDownBpCommand _copyDownBpCommand_;

        public ACopydownbpCmd()
        {
        }

        public ACopydownbpCmd(PCopyDownBpCommand _copyDownBpCommand_)
        {
            SetCopyDownBpCommand(_copyDownBpCommand_);
        }

        public override object Clone()
        {
            return (object)new ACopydownbpCmd((PCopyDownBpCommand)CloneNode(_copyDownBpCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseACopydownbpCmd(this);
        }

        public PCopyDownBpCommand GetCopyDownBpCommand()
        {
            return _copyDownBpCommand_;
        }

        public void SetCopyDownBpCommand(PCopyDownBpCommand node)
        {
            if (_copyDownBpCommand_ != null)
            {
                _copyDownBpCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _copyDownBpCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_copyDownBpCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_copyDownBpCommand_ == child)
            {
                _copyDownBpCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_copyDownBpCommand_ == oldChild)
            {
                SetCopyDownBpCommand((PCopyDownBpCommand)newChild);
            }
        }
    }

}
