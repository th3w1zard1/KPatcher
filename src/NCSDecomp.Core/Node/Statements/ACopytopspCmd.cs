// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ACopytopspCmd : PCmd
    {

        private PCopyTopSpCommand _copyTopSpCommand_;

        public ACopytopspCmd()
        {
        }

        public ACopytopspCmd(PCopyTopSpCommand _copyTopSpCommand_)
        {
            SetCopyTopSpCommand(_copyTopSpCommand_);
        }

        public override object Clone()
        {
            return (object)new ACopytopspCmd((PCopyTopSpCommand)CloneNode(_copyTopSpCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseACopytopspCmd(this);
        }

        public PCopyTopSpCommand GetCopyTopSpCommand()
        {
            return _copyTopSpCommand_;
        }

        public void SetCopyTopSpCommand(PCopyTopSpCommand node)
        {
            if (_copyTopSpCommand_ != null)
            {
                _copyTopSpCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _copyTopSpCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_copyTopSpCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_copyTopSpCommand_ == child)
            {
                _copyTopSpCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_copyTopSpCommand_ == oldChild)
            {
                SetCopyTopSpCommand((PCopyTopSpCommand)newChild);
            }
        }
    }

}
