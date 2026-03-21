// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AStackOpCmd : PCmd
    {

        private PStackCommand _stackCommand_;

        public AStackOpCmd()
        {
        }

        public AStackOpCmd(PStackCommand _stackCommand_)
        {
            SetStackCommand(_stackCommand_);
        }

        public override object Clone()
        {
            PStackCommand clonedStackCommand = _stackCommand_ != null ? (PStackCommand)_stackCommand_.Clone() : null;
            return (object)new AStackOpCmd(clonedStackCommand);
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAStackOpCmd(this);
        }

        public PStackCommand GetStackCommand()
        {
            return _stackCommand_;
        }

        public void SetStackCommand(PStackCommand node)
        {
            if (_stackCommand_ != null)
            {
                _stackCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _stackCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_stackCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_stackCommand_ == child)
            {
                _stackCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_stackCommand_ == oldChild)
            {
                SetStackCommand((PStackCommand)newChild);
            }
        }
    }

}
