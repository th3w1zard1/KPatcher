// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AActionCmd : PCmd
    {

        private PActionCommand _actionCommand_;

        public AActionCmd()
        {
        }

        public AActionCmd(PActionCommand _actionCommand_)
        {
            SetActionCommand(_actionCommand_);
        }

        public override object Clone()
        {
            return (object)new AActionCmd((PActionCommand)CloneNode(_actionCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAActionCmd(this);
        }

        public PActionCommand GetActionCommand()
        {
            return _actionCommand_;
        }

        public void SetActionCommand(PActionCommand node)
        {
            if (_actionCommand_ != null)
            {
                _actionCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _actionCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_actionCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_actionCommand_ == child)
            {
                _actionCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_actionCommand_ == oldChild)
            {
                SetActionCommand((PActionCommand)newChild);
            }
        }
    }

}
