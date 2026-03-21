// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AJumpSubCmd : PCmd
    {

        private PJumpToSubroutine _jumpToSubroutine_;

        public AJumpSubCmd()
        {
        }

        public AJumpSubCmd(PJumpToSubroutine _jumpToSubroutine_)
        {
            SetJumpToSubroutine(_jumpToSubroutine_);
        }

        public override object Clone()
        {
            return (object)new AJumpSubCmd((PJumpToSubroutine)CloneNode(_jumpToSubroutine_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAJumpSubCmd(this);
        }

        public PJumpToSubroutine GetJumpToSubroutine()
        {
            return _jumpToSubroutine_;
        }

        public void SetJumpToSubroutine(PJumpToSubroutine node)
        {
            if (_jumpToSubroutine_ != null)
            {
                _jumpToSubroutine_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _jumpToSubroutine_ = node;
        }

        public override string ToString()
        {
            return ToString(_jumpToSubroutine_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_jumpToSubroutine_ == child)
            {
                _jumpToSubroutine_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_jumpToSubroutine_ == oldChild)
            {
                SetJumpToSubroutine((PJumpToSubroutine)newChild);
            }
        }
    }

}
