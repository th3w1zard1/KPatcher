// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AJumpCmd : PCmd
    {

        private PJumpCommand _jumpCommand_;

        public AJumpCmd()
        {
        }

        public AJumpCmd(PJumpCommand _jumpCommand_)
        {
            SetJumpCommand(_jumpCommand_);
        }

        public override object Clone()
        {
            return (object)new AJumpCmd((PJumpCommand)CloneNode(_jumpCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAJumpCmd(this);
        }

        public PJumpCommand GetJumpCommand()
        {
            return _jumpCommand_;
        }

        public void SetJumpCommand(PJumpCommand node)
        {
            if (_jumpCommand_ != null)
            {
                _jumpCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _jumpCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_jumpCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_jumpCommand_ == child)
            {
                _jumpCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_jumpCommand_ == oldChild)
            {
                SetJumpCommand((PJumpCommand)newChild);
            }
        }
    }

}
