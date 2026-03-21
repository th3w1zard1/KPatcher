// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ACondJumpCmd : PCmd
    {

        private PConditionalJumpCommand _conditionalJumpCommand_;

        public ACondJumpCmd()
        {
        }

        public ACondJumpCmd(PConditionalJumpCommand _conditionalJumpCommand_)
        {
            SetConditionalJumpCommand(_conditionalJumpCommand_);
        }

        public override object Clone()
        {
            return (object)new ACondJumpCmd((PConditionalJumpCommand)CloneNode(_conditionalJumpCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseACondJumpCmd(this);
        }

        public PConditionalJumpCommand GetConditionalJumpCommand()
        {
            return _conditionalJumpCommand_;
        }

        public void SetConditionalJumpCommand(PConditionalJumpCommand node)
        {
            if (_conditionalJumpCommand_ != null)
            {
                _conditionalJumpCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _conditionalJumpCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_conditionalJumpCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_conditionalJumpCommand_ == child)
            {
                _conditionalJumpCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_conditionalJumpCommand_ == oldChild)
            {
                SetConditionalJumpCommand((PConditionalJumpCommand)newChild);
            }
        }
    }

}
