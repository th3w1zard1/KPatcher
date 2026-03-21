// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AUnaryCommand : PUnaryCommand
    {

        private PUnaryOp _unaryOp_;
        private TIntegerConstant _pos_;
        private TIntegerConstant _type_;
        private TSemi _semi_;

        public AUnaryCommand()
        {
        }

        public AUnaryCommand(PUnaryOp _unaryOp_, TIntegerConstant _pos_, TIntegerConstant _type_, TSemi _semi_)
        {
            SetUnaryOp(_unaryOp_);
            SetPos(_pos_);
            SetType(_type_);
            SetSemi(_semi_);
        }

        public override object Clone()
        {
            return (object)new AUnaryCommand(
             (PUnaryOp)CloneNode(_unaryOp_),
             (TIntegerConstant)CloneNode(_pos_),
             (TIntegerConstant)CloneNode(_type_),
             (TSemi)CloneNode(_semi_)
          );
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAUnaryCommand(this);
        }

        public PUnaryOp GetUnaryOp()
        {
            return _unaryOp_;
        }

        public void SetUnaryOp(PUnaryOp node)
        {
            if (_unaryOp_ != null)
            {
                _unaryOp_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _unaryOp_ = node;
        }

        public TIntegerConstant GetPos()
        {
            return _pos_;
        }

        public void SetPos(TIntegerConstant node)
        {
            if (_pos_ != null)
            {
                _pos_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _pos_ = node;
        }

        public TIntegerConstant GetCmdType()
        {
            return _type_;
        }

        public void SetType(TIntegerConstant node)
        {
            if (_type_ != null)
            {
                _type_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _type_ = node;
        }

        public TSemi GetSemi()
        {
            return _semi_;
        }

        public void SetSemi(TSemi node)
        {
            if (_semi_ != null)
            {
                _semi_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _semi_ = node;
        }

        public override string ToString()
        {
            return ToString(_unaryOp_) + ToString(_pos_) + ToString(_type_) + ToString(_semi_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_unaryOp_ == child)
            {
                _unaryOp_ = null;
            }
            else if (_pos_ == child)
            {
                _pos_ = null;
            }
            else if (_type_ == child)
            {
                _type_ = null;
            }
            else if (_semi_ == child)
            {
                _semi_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_unaryOp_ == oldChild)
            {
                SetUnaryOp((PUnaryOp)newChild);
            }
            else if (_pos_ == oldChild)
            {
                SetPos((TIntegerConstant)newChild);
            }
            else if (_type_ == oldChild)
            {
                SetType((TIntegerConstant)newChild);
            }
            else if (_semi_ == oldChild)
            {
                SetSemi((TSemi)newChild);
            }
        }
    }

}
