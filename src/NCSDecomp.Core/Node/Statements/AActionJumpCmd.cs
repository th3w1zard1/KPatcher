// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AActionJumpCmd : PCmd
    {

        private PStoreStateCommand _storeStateCommand_;
        private PJumpCommand _jumpCommand_;
        private PCommandBlock _commandBlock_;
        private PReturn _return_;

        public AActionJumpCmd()
        {
        }

        public AActionJumpCmd(PStoreStateCommand _storeStateCommand_, PJumpCommand _jumpCommand_, PCommandBlock _commandBlock_, PReturn _return_)
        {
            SetStoreStateCommand(_storeStateCommand_);
            SetJumpCommand(_jumpCommand_);
            SetCommandBlock(_commandBlock_);
            SetReturn(_return_);
        }

        public override object Clone()
        {
            return (object)new AActionJumpCmd(
             (PStoreStateCommand)CloneNode(_storeStateCommand_),
             (PJumpCommand)CloneNode(_jumpCommand_),
             (PCommandBlock)CloneNode(_commandBlock_),
             (PReturn)CloneNode(_return_)
          );
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAActionJumpCmd(this);
        }

        public PStoreStateCommand GetStoreStateCommand()
        {
            return _storeStateCommand_;
        }

        public void SetStoreStateCommand(PStoreStateCommand node)
        {
            if (_storeStateCommand_ != null)
            {
                _storeStateCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _storeStateCommand_ = node;
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
            return ToString(_storeStateCommand_) + ToString(_jumpCommand_) + ToString(_commandBlock_) + ToString(_return_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_storeStateCommand_ == child)
            {
                _storeStateCommand_ = null;
            }
            else if (_jumpCommand_ == child)
            {
                _jumpCommand_ = null;
            }
            else if (_commandBlock_ == child)
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
            if (_storeStateCommand_ == oldChild)
            {
                SetStoreStateCommand((PStoreStateCommand)newChild);
            }
            else if (_jumpCommand_ == oldChild)
            {
                SetJumpCommand((PJumpCommand)newChild);
            }
            else if (_commandBlock_ == oldChild)
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
