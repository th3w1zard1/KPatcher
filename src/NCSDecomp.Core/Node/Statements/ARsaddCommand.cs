// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ARsaddCommand : PRsaddCommand
    {

        private TRsadd _rsadd_;
        private TIntegerConstant _pos_;
        private TIntegerConstant _type_;
        private TSemi _semi_;

        public ARsaddCommand()
        {
        }

        public ARsaddCommand(TRsadd _rsadd_, TIntegerConstant _pos_, TIntegerConstant _type_, TSemi _semi_)
        {
            SetRsadd(_rsadd_);
            SetPos(_pos_);
            SetType(_type_);
            SetSemi(_semi_);
        }

        public override object Clone()
        {
            return (object)new ARsaddCommand(
             (TRsadd)CloneNode(_rsadd_),
             (TIntegerConstant)CloneNode(_pos_),
             (TIntegerConstant)CloneNode(_type_),
             (TSemi)CloneNode(_semi_)
          );
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseARsaddCommand(this);
        }

        public TRsadd GetRsadd()
        {
            return _rsadd_;
        }

        public void SetRsadd(TRsadd node)
        {
            if (_rsadd_ != null)
            {
                _rsadd_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _rsadd_ = node;
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
            return ToString(_rsadd_) + ToString(_pos_) + ToString(_type_) + ToString(_semi_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_rsadd_ == child)
            {
                _rsadd_ = null;
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
            if (_rsadd_ == oldChild)
            {
                SetRsadd((TRsadd)newChild);
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
