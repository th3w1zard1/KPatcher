// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ASize : PSize
    {

        private TT _t_;
        private TIntegerConstant _pos_;
        private TIntegerConstant _integerConstant_;
        private TSemi _semi_;

        public ASize()
        {
        }

        public ASize(TT _t_, TIntegerConstant _pos_, TIntegerConstant _integerConstant_, TSemi _semi_)
        {
            SetT(_t_);
            SetPos(_pos_);
            SetIntegerConstant(_integerConstant_);
            SetSemi(_semi_);
        }

        public override object Clone()
        {
            TT clonedT = _t_ != null ? (TT)CloneNode(_t_) : null;
            TIntegerConstant clonedPos = _pos_ != null ? (TIntegerConstant)CloneNode(_pos_) : null;
            TIntegerConstant clonedIntegerConstant = _integerConstant_ != null ? (TIntegerConstant)CloneNode(_integerConstant_) : null;
            TSemi clonedSemi = _semi_ != null ? (TSemi)CloneNode(_semi_) : null;
            return (object)new ASize(clonedT, clonedPos, clonedIntegerConstant, clonedSemi);
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseASize(this);
        }

        public TT GetT()
        {
            return _t_;
        }

        public void SetT(TT node)
        {
            if (_t_ != null)
            {
                _t_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _t_ = node;
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

        public TIntegerConstant GetIntegerConstant()
        {
            return _integerConstant_;
        }

        public void SetIntegerConstant(TIntegerConstant node)
        {
            if (_integerConstant_ != null)
            {
                _integerConstant_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _integerConstant_ = node;
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
            return ToString(_t_) + ToString(_pos_) + ToString(_integerConstant_) + ToString(_semi_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_t_ == child)
            {
                _t_ = null;
            }
            else if (_pos_ == child)
            {
                _pos_ = null;
            }
            else if (_integerConstant_ == child)
            {
                _integerConstant_ = null;
            }
            else if (_semi_ == child)
            {
                _semi_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_t_ == oldChild)
            {
                SetT((TT)newChild);
            }
            else if (_pos_ == oldChild)
            {
                SetPos((TIntegerConstant)newChild);
            }
            else if (_integerConstant_ == oldChild)
            {
                SetIntegerConstant((TIntegerConstant)newChild);
            }
            else if (_semi_ == oldChild)
            {
                SetSemi((TSemi)newChild);
            }
        }
    }

}
