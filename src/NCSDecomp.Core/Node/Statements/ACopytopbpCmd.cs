// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ACopytopbpCmd : PCmd
    {

        private PCopyTopBpCommand _copyTopBpCommand_;

        public ACopytopbpCmd()
        {
        }

        public ACopytopbpCmd(PCopyTopBpCommand _copyTopBpCommand_)
        {
            SetCopyTopBpCommand(_copyTopBpCommand_);
        }

        public override object Clone()
        {
            return (object)new ACopytopbpCmd((PCopyTopBpCommand)CloneNode(_copyTopBpCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseACopytopbpCmd(this);
        }

        public PCopyTopBpCommand GetCopyTopBpCommand()
        {
            return _copyTopBpCommand_;
        }

        public void SetCopyTopBpCommand(PCopyTopBpCommand node)
        {
            if (_copyTopBpCommand_ != null)
            {
                _copyTopBpCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _copyTopBpCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_copyTopBpCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_copyTopBpCommand_ == child)
            {
                _copyTopBpCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_copyTopBpCommand_ == oldChild)
            {
                SetCopyTopBpCommand((PCopyTopBpCommand)newChild);
            }
        }
    }

}
