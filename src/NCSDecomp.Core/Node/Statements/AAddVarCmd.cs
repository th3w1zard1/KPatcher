// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AAddVarCmd : PCmd
    {

        private PRsaddCommand _rsaddCommand_;

        public AAddVarCmd()
        {
        }

        public AAddVarCmd(PRsaddCommand _rsaddCommand_)
        {
            SetRsaddCommand(_rsaddCommand_);
        }

        public override object Clone()
        {
            return (object)new AAddVarCmd((PRsaddCommand)CloneNode(_rsaddCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAAddVarCmd(this);
        }

        public PRsaddCommand GetRsaddCommand()
        {
            return _rsaddCommand_;
        }

        public void SetRsaddCommand(PRsaddCommand node)
        {
            if (_rsaddCommand_ != null)
            {
                _rsaddCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _rsaddCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_rsaddCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_rsaddCommand_ == child)
            {
                _rsaddCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_rsaddCommand_ == oldChild)
            {
                SetRsaddCommand((PRsaddCommand)newChild);
            }
        }
    }

}
