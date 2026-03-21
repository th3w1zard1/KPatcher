// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AConstCmd : PCmd
    {

        private PConstCommand _constCommand_;

        public AConstCmd()
        {
        }

        public AConstCmd(PConstCommand _constCommand_)
        {
            SetConstCommand(_constCommand_);
        }

        public override object Clone()
        {
            return (object)new AConstCmd((PConstCommand)CloneNode(_constCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAConstCmd(this);
        }

        public PConstCommand GetConstCommand()
        {
            return _constCommand_;
        }

        public void SetConstCommand(PConstCommand node)
        {
            if (_constCommand_ != null)
            {
                _constCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _constCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_constCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_constCommand_ == child)
            {
                _constCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_constCommand_ == oldChild)
            {
                SetConstCommand((PConstCommand)newChild);
            }
        }
    }

}
