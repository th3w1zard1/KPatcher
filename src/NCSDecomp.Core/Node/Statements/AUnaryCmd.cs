// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AUnaryCmd : PCmd
    {

        private PUnaryCommand _unaryCommand_;

        public AUnaryCmd()
        {
        }

        public AUnaryCmd(PUnaryCommand _unaryCommand_)
        {
            SetUnaryCommand(_unaryCommand_);
        }

        public override object Clone()
        {
            PUnaryCommand clonedUnaryCommand = _unaryCommand_ != null ? (PUnaryCommand)_unaryCommand_.Clone() : null;
            return (object)new AUnaryCmd(clonedUnaryCommand);
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAUnaryCmd(this);
        }

        public PUnaryCommand GetUnaryCommand()
        {
            return _unaryCommand_;
        }

        public void SetUnaryCommand(PUnaryCommand node)
        {
            if (_unaryCommand_ != null)
            {
                _unaryCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _unaryCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_unaryCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_unaryCommand_ == child)
            {
                _unaryCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_unaryCommand_ == oldChild)
            {
                SetUnaryCommand((PUnaryCommand)newChild);
            }
        }
    }

}
