// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AStackCommand : PStackCommand
    {

        private PStackOp _stackOp_;
        private TIntegerConstant _pos_;
        private TIntegerConstant _type_;
        private TIntegerConstant _offset_;
        private TSemi _semi_;

        public AStackCommand()
        {
        }

        public AStackCommand(PStackOp _stackOp_, TIntegerConstant _pos_, TIntegerConstant _type_, TIntegerConstant _offset_, TSemi _semi_)
        {
            SetStackOp(_stackOp_);
            SetPos(_pos_);
            SetType(_type_);
            SetOffset(_offset_);
            SetSemi(_semi_);
        }

        public override object Clone()
        {
            PStackOp clonedStackOp = _stackOp_ != null ? (PStackOp)_stackOp_.Clone() : null;
            TIntegerConstant clonedPos = _pos_ != null ? (TIntegerConstant)CloneNode(_pos_) : null;
            TIntegerConstant clonedType = _type_ != null ? (TIntegerConstant)CloneNode(_type_) : null;
            TIntegerConstant clonedOffset = _offset_ != null ? (TIntegerConstant)CloneNode(_offset_) : null;
            TSemi clonedSemi = _semi_ != null ? (TSemi)CloneNode(_semi_) : null;
            return (object)new AStackCommand(clonedStackOp, clonedPos, clonedType, clonedOffset, clonedSemi);
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAStackCommand(this);
        }

        public PStackOp GetStackOp()
        {
            return _stackOp_;
        }

        public void SetStackOp(PStackOp node)
        {
            if (_stackOp_ != null)
            {
                _stackOp_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _stackOp_ = node;
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

        public TIntegerConstant GetOffset()
        {
            return _offset_;
        }

        public void SetOffset(TIntegerConstant node)
        {
            if (_offset_ != null)
            {
                _offset_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _offset_ = node;
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
            return ToString(_stackOp_) + ToString(_pos_) + ToString(_type_) + ToString(_offset_) + ToString(_semi_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_stackOp_ == child)
            {
                _stackOp_ = null;
            }
            else if (_pos_ == child)
            {
                _pos_ = null;
            }
            else if (_type_ == child)
            {
                _type_ = null;
            }
            else if (_offset_ == child)
            {
                _offset_ = null;
            }
            else if (_semi_ == child)
            {
                _semi_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_stackOp_ == oldChild)
            {
                SetStackOp((PStackOp)newChild);
            }
            else if (_pos_ == oldChild)
            {
                SetPos((TIntegerConstant)newChild);
            }
            else if (_type_ == oldChild)
            {
                SetType((TIntegerConstant)newChild);
            }
            else if (_offset_ == oldChild)
            {
                SetOffset((TIntegerConstant)newChild);
            }
            else if (_semi_ == oldChild)
            {
                SetSemi((TSemi)newChild);
            }
        }
    }

}
