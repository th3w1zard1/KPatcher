// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AMovespCmd : PCmd
    {

        private PMoveSpCommand _moveSpCommand_;

        public AMovespCmd()
        {
        }

        public AMovespCmd(PMoveSpCommand _moveSpCommand_)
        {
            SetMoveSpCommand(_moveSpCommand_);
        }

        public override object Clone()
        {
            PMoveSpCommand clonedMoveSpCommand = _moveSpCommand_ != null ? (PMoveSpCommand)_moveSpCommand_.Clone() : null;
            return (object)new AMovespCmd(clonedMoveSpCommand);
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAMovespCmd(this);
        }

        public PMoveSpCommand GetMoveSpCommand()
        {
            return _moveSpCommand_;
        }

        public void SetMoveSpCommand(PMoveSpCommand node)
        {
            if (_moveSpCommand_ != null)
            {
                _moveSpCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _moveSpCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_moveSpCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_moveSpCommand_ == child)
            {
                _moveSpCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_moveSpCommand_ == oldChild)
            {
                SetMoveSpCommand((PMoveSpCommand)newChild);
            }
        }
    }

}
