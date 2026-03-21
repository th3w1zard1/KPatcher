// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ASubroutine : PSubroutine
    {

        private PCommandBlock _commandBlock_;
        private PReturn _return_;

        public ASubroutine()
        {
        }

        public ASubroutine(PCommandBlock _commandBlock_, PReturn _return_)
        {
            SetCommandBlock(_commandBlock_);
            SetReturn(_return_);
        }

        public override object Clone()
        {
            PCommandBlock clonedCommandBlock = _commandBlock_ != null ? (PCommandBlock)_commandBlock_.Clone() : null;
            PReturn clonedReturn = _return_ != null ? (PReturn)_return_.Clone() : null;
            return (object)new ASubroutine(clonedCommandBlock, clonedReturn);
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseASubroutine(this);
        }

        public PCommandBlock GetCommandBlock()
        {
            return _commandBlock_;
        }

        public void SetCommandBlock(PCommandBlock node)
        {
            if (_commandBlock_ != null)
            {
                _commandBlock_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _commandBlock_ = node;
        }

        public PReturn GetReturn()
        {
            return _return_;
        }

        public void SetReturn(PReturn node)
        {
            if (_return_ != null)
            {
                _return_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _return_ = node;
        }

        public override string ToString()
        {
            return ToString(_commandBlock_) + ToString(_return_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_commandBlock_ == child)
            {
                _commandBlock_ = null;
            }
            else if (_return_ == child)
            {
                _return_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_commandBlock_ == oldChild)
            {
                SetCommandBlock((PCommandBlock)newChild);
            }
            else if (_return_ == oldChild)
            {
                SetReturn((PReturn)newChild);
            }
        }
    }

}
