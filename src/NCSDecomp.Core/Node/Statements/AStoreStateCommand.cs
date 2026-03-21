// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AStoreStateCommand : PStoreStateCommand
    {

        private TStorestate _storestate_;
        private TIntegerConstant _pos_;
        private TIntegerConstant _offset_;
        private TIntegerConstant _sizeBp_;
        private TIntegerConstant _sizeSp_;
        private TSemi _semi_;

        public AStoreStateCommand()
        {
        }

        public AStoreStateCommand(
           TStorestate _storestate_, TIntegerConstant _pos_, TIntegerConstant _offset_, TIntegerConstant _sizeBp_, TIntegerConstant _sizeSp_, TSemi _semi_
        )
        {
            SetStorestate(_storestate_);
            SetPos(_pos_);
            SetOffset(_offset_);
            SetSizeBp(_sizeBp_);
            SetSizeSp(_sizeSp_);
            SetSemi(_semi_);
        }

        public override object Clone()
        {
            return (object)new AStoreStateCommand(
             (TStorestate)CloneNode(_storestate_),
             (TIntegerConstant)CloneNode(_pos_),
             (TIntegerConstant)CloneNode(_offset_),
             (TIntegerConstant)CloneNode(_sizeBp_),
             (TIntegerConstant)CloneNode(_sizeSp_),
             (TSemi)CloneNode(_semi_)
          );
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAStoreStateCommand(this);
        }

        public TStorestate GetStorestate()
        {
            return _storestate_;
        }

        public void SetStorestate(TStorestate node)
        {
            if (_storestate_ != null)
            {
                _storestate_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _storestate_ = node;
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

        public TIntegerConstant GetSizeBp()
        {
            return _sizeBp_;
        }

        public void SetSizeBp(TIntegerConstant node)
        {
            if (_sizeBp_ != null)
            {
                _sizeBp_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _sizeBp_ = node;
        }

        public TIntegerConstant GetSizeSp()
        {
            return _sizeSp_;
        }

        public void SetSizeSp(TIntegerConstant node)
        {
            if (_sizeSp_ != null)
            {
                _sizeSp_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _sizeSp_ = node;
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
            return ToString(_storestate_)
               + ToString(_pos_)
               + ToString(_offset_)
               + ToString(_sizeBp_)
               + ToString(_sizeSp_)
               + ToString(_semi_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_storestate_ == child)
            {
                _storestate_ = null;
            }
            else if (_pos_ == child)
            {
                _pos_ = null;
            }
            else if (_offset_ == child)
            {
                _offset_ = null;
            }
            else if (_sizeBp_ == child)
            {
                _sizeBp_ = null;
            }
            else if (_sizeSp_ == child)
            {
                _sizeSp_ = null;
            }
            else if (_semi_ == child)
            {
                _semi_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_storestate_ == oldChild)
            {
                SetStorestate((TStorestate)newChild);
            }
            else if (_pos_ == oldChild)
            {
                SetPos((TIntegerConstant)newChild);
            }
            else if (_offset_ == oldChild)
            {
                SetOffset((TIntegerConstant)newChild);
            }
            else if (_sizeBp_ == oldChild)
            {
                SetSizeBp((TIntegerConstant)newChild);
            }
            else if (_sizeSp_ == oldChild)
            {
                SetSizeSp((TIntegerConstant)newChild);
            }
            else if (_semi_ == oldChild)
            {
                SetSemi((TSemi)newChild);
            }
        }
    }

}
