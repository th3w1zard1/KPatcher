// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class ADestructCommand : PDestructCommand
    {

        private TDestruct _destruct_;
        private TIntegerConstant _pos_;
        private TIntegerConstant _type_;
        private TIntegerConstant _sizeRem_;
        private TIntegerConstant _offset_;
        private TIntegerConstant _sizeSave_;
        private TSemi _semi_;

        public ADestructCommand()
        {
        }

        public ADestructCommand(
           TDestruct _destruct_,
           TIntegerConstant _pos_,
           TIntegerConstant _type_,
           TIntegerConstant _sizeRem_,
           TIntegerConstant _offset_,
           TIntegerConstant _sizeSave_,
           TSemi _semi_
        )
        {
            SetDestruct(_destruct_);
            SetPos(_pos_);
            SetType(_type_);
            SetSizeRem(_sizeRem_);
            SetOffset(_offset_);
            SetSizeSave(_sizeSave_);
            SetSemi(_semi_);
        }

        public override object Clone()
        {
            return (object)new ADestructCommand(
             (TDestruct)CloneNode(_destruct_),
             (TIntegerConstant)CloneNode(_pos_),
             (TIntegerConstant)CloneNode(_type_),
             (TIntegerConstant)CloneNode(_sizeRem_),
             (TIntegerConstant)CloneNode(_offset_),
             (TIntegerConstant)CloneNode(_sizeSave_),
             (TSemi)CloneNode(_semi_)
          );
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseADestructCommand(this);
        }

        public TDestruct GetDestruct()
        {
            return _destruct_;
        }

        public void SetDestruct(TDestruct node)
        {
            if (_destruct_ != null)
            {
                _destruct_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _destruct_ = node;
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

        public TIntegerConstant GetSizeRem()
        {
            return _sizeRem_;
        }

        public void SetSizeRem(TIntegerConstant node)
        {
            if (_sizeRem_ != null)
            {
                _sizeRem_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _sizeRem_ = node;
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

        public TIntegerConstant GetSizeSave()
        {
            return _sizeSave_;
        }

        public void SetSizeSave(TIntegerConstant node)
        {
            if (_sizeSave_ != null)
            {
                _sizeSave_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _sizeSave_ = node;
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
            return ToString(_destruct_)
               + ToString(_pos_)
               + ToString(_type_)
               + ToString(_sizeRem_)
               + ToString(_offset_)
               + ToString(_sizeSave_)
               + ToString(_semi_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_destruct_ == child)
            {
                _destruct_ = null;
            }
            else if (_pos_ == child)
            {
                _pos_ = null;
            }
            else if (_type_ == child)
            {
                _type_ = null;
            }
            else if (_sizeRem_ == child)
            {
                _sizeRem_ = null;
            }
            else if (_offset_ == child)
            {
                _offset_ = null;
            }
            else if (_sizeSave_ == child)
            {
                _sizeSave_ = null;
            }
            else if (_semi_ == child)
            {
                _semi_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_destruct_ == oldChild)
            {
                SetDestruct((TDestruct)newChild);
            }
            else if (_pos_ == oldChild)
            {
                SetPos((TIntegerConstant)newChild);
            }
            else if (_type_ == oldChild)
            {
                SetType((TIntegerConstant)newChild);
            }
            else if (_sizeRem_ == oldChild)
            {
                SetSizeRem((TIntegerConstant)newChild);
            }
            else if (_offset_ == oldChild)
            {
                SetOffset((TIntegerConstant)newChild);
            }
            else if (_sizeSave_ == oldChild)
            {
                SetSizeSave((TIntegerConstant)newChild);
            }
            else if (_semi_ == oldChild)
            {
                SetSemi((TSemi)newChild);
            }
        }
    }

}
