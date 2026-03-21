// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AStoreStateCmd : PCmd
    {

        private PStoreStateCommand _storeStateCommand_;

        public AStoreStateCmd()
        {
        }

        public AStoreStateCmd(PStoreStateCommand _storeStateCommand_)
        {
            SetStoreStateCommand(_storeStateCommand_);
        }

        public override object Clone()
        {
            return (object)new AStoreStateCmd((PStoreStateCommand)CloneNode(_storeStateCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAStoreStateCmd(this);
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

        public override string ToString()
        {
            return ToString(_storeStateCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_storeStateCommand_ == child)
            {
                _storeStateCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_storeStateCommand_ == oldChild)
            {
                SetStoreStateCommand((PStoreStateCommand)newChild);
            }
        }
    }

}
