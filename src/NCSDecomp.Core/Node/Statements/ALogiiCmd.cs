// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ALogiiCmd : PCmd
    {

        private PLogiiCommand _logiiCommand_;

        public ALogiiCmd()
        {
        }

        public ALogiiCmd(PLogiiCommand _logiiCommand_)
        {
            SetLogiiCommand(_logiiCommand_);
        }

        public override object Clone()
        {
            return (object)new ALogiiCmd((PLogiiCommand)CloneNode(_logiiCommand_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseALogiiCmd(this);
        }

        public PLogiiCommand GetLogiiCommand()
        {
            return _logiiCommand_;
        }

        public void SetLogiiCommand(PLogiiCommand node)
        {
            if (_logiiCommand_ != null)
            {
                _logiiCommand_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _logiiCommand_ = node;
        }

        public override string ToString()
        {
            return ToString(_logiiCommand_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_logiiCommand_ == child)
            {
                _logiiCommand_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_logiiCommand_ == oldChild)
            {
                SetLogiiCommand((PLogiiCommand)newChild);
            }
        }
    }

}
