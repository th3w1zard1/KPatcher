// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ADestructCmd : PCmd
    {

        private PDestructCommand _destructCommand_;

        public ADestructCmd()
        {
        }

        public ADestructCmd(PDestructCommand _destructCommand_)
        {
            SetDestructCommand(_destructCommand_);
        }

        public override object Clone()
        {
            return (object)new ADestructCmd((PDestructCommand)CloneNode(_destructCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseADestructCmd(this);
        }

        public PDestructCommand GetDestructCommand()
        {
            return _destructCommand_;
        }

        public void SetDestructCommand(PDestructCommand node)
        {
            if (_destructCommand_ != null)
            {
                _destructCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _destructCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_destructCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_destructCommand_ == child)
            {
                _destructCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_destructCommand_ == oldChild)
            {
                SetDestructCommand((PDestructCommand)newChild);
            }
        }
    }

}
