// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ACopyDownBpCommand : PCopyDownBpCommand
    {

        private TCpdownbp _cpdownbp_;
        private TIntegerConstant _pos_;
        private TIntegerConstant _type_;
        private TIntegerConstant _offset_;
        private TIntegerConstant _size_;
        private TSemi _semi_;

        public ACopyDownBpCommand()
        {
        }

        public ACopyDownBpCommand(
           TCpdownbp _cpdownbp_, TIntegerConstant _pos_, TIntegerConstant _type_, TIntegerConstant _offset_, TIntegerConstant _size_, TSemi _semi_
        )
        {
            SetCpdownbp(_cpdownbp_);
            SetPos(_pos_);
            SetType(_type_);
            SetOffset(_offset_);
            SetSize(_size_);
            SetSemi(_semi_);
        }

        public override object Clone()
        {
            return (object)new ACopyDownBpCommand(
             (TCpdownbp)CloneNode(_cpdownbp_),
             (TIntegerConstant)CloneNode(_pos_),
             (TIntegerConstant)CloneNode(_type_),
             (TIntegerConstant)CloneNode(_offset_),
             (TIntegerConstant)CloneNode(_size_),
             (TSemi)CloneNode(_semi_)
          );
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseACopyDownBpCommand(this);
        }

        public TCpdownbp GetCpdownbp()
        {
            return _cpdownbp_;
        }

        public void SetCpdownbp(TCpdownbp node)
        {
            if (_cpdownbp_ != null)
            {
                _cpdownbp_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _cpdownbp_ = node;
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

        public TIntegerConstant GetSize()
        {
            return _size_;
        }

        public void SetSize(TIntegerConstant node)
        {
            if (_size_ != null)
            {
                _size_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _size_ = node;
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
            return ToString(_cpdownbp_)
               + ToString(_pos_)
               + ToString(_type_)
               + ToString(_offset_)
               + ToString(_size_)
               + ToString(_semi_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_cpdownbp_ == child)
            {
                _cpdownbp_ = null;
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
            else if (_size_ == child)
            {
                _size_ = null;
            }
            else if (_semi_ == child)
            {
                _semi_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_cpdownbp_ == oldChild)
            {
                SetCpdownbp((TCpdownbp)newChild);
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
            else if (_size_ == oldChild)
            {
                SetSize((TIntegerConstant)newChild);
            }
            else if (_semi_ == oldChild)
            {
                SetSemi((TSemi)newChild);
            }
        }
    }

}
