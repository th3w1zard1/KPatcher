// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ACopydownspCmd : PCmd
    {

        private PCopyDownSpCommand _copyDownSpCommand_;

        public ACopydownspCmd()
        {
        }

        public ACopydownspCmd(PCopyDownSpCommand _copyDownSpCommand_)
        {
            SetCopyDownSpCommand(_copyDownSpCommand_);
        }

        public override object Clone()
        {
            return (object)new ACopydownspCmd((PCopyDownSpCommand)CloneNode(_copyDownSpCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseACopydownspCmd(this);
        }

        public PCopyDownSpCommand GetCopyDownSpCommand()
        {
            return _copyDownSpCommand_;
        }

        public void SetCopyDownSpCommand(PCopyDownSpCommand node)
        {
            if (_copyDownSpCommand_ != null)
            {
                _copyDownSpCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _copyDownSpCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_copyDownSpCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_copyDownSpCommand_ == child)
            {
                _copyDownSpCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_copyDownSpCommand_ == oldChild)
            {
                SetCopyDownSpCommand((PCopyDownSpCommand)newChild);
            }
        }
    }

}
