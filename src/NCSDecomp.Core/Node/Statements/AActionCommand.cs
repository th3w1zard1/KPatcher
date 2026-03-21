// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class AActionCommand : PActionCommand
    {

        private TAction _action_;
        private TIntegerConstant _pos_;
        private TIntegerConstant _type_;
        private TIntegerConstant _id_;
        private TIntegerConstant _argCount_;
        private TSemi _semi_;

        public AActionCommand()
        {
        }

        public AActionCommand(TAction _action_, TIntegerConstant _pos_, TIntegerConstant _type_, TIntegerConstant _id_, TIntegerConstant _argCount_, TSemi _semi_)
        {
            SetAction(_action_);
            SetPos(_pos_);
            SetType(_type_);
            SetId(_id_);
            SetArgCount(_argCount_);
            SetSemi(_semi_);
        }

        public override object Clone()
        {
            return (object)new AActionCommand(
             (TAction)CloneNode(_action_),
             (TIntegerConstant)CloneNode(_pos_),
             (TIntegerConstant)CloneNode(_type_),
             (TIntegerConstant)CloneNode(_id_),
             (TIntegerConstant)CloneNode(_argCount_),
             (TSemi)CloneNode(_semi_)
          );
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAActionCommand(this);
        }

        public TAction GetAction()
        {
            return _action_;
        }

        public void SetAction(TAction node)
        {
            if (_action_ != null)
            {
                _action_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _action_ = node;
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

        public TIntegerConstant GetId()
        {
            return _id_;
        }

        public void SetId(TIntegerConstant node)
        {
            if (_id_ != null)
            {
                _id_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _id_ = node;
        }

        public TIntegerConstant GetArgCount()
        {
            return _argCount_;
        }

        public void SetArgCount(TIntegerConstant node)
        {
            if (_argCount_ != null)
            {
                _argCount_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _argCount_ = node;
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
            return ToString(_action_)
               + ToString(_pos_)
               + ToString(_type_)
               + ToString(_id_)
               + ToString(_argCount_)
               + ToString(_semi_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_action_ == child)
            {
                _action_ = null;
            }
            else if (_pos_ == child)
            {
                _pos_ = null;
            }
            else if (_type_ == child)
            {
                _type_ = null;
            }
            else if (_id_ == child)
            {
                _id_ = null;
            }
            else if (_argCount_ == child)
            {
                _argCount_ = null;
            }
            else if (_semi_ == child)
            {
                _semi_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_action_ == oldChild)
            {
                SetAction((TAction)newChild);
            }
            else if (_pos_ == oldChild)
            {
                SetPos((TIntegerConstant)newChild);
            }
            else if (_type_ == oldChild)
            {
                SetType((TIntegerConstant)newChild);
            }
            else if (_id_ == oldChild)
            {
                SetId((TIntegerConstant)newChild);
            }
            else if (_argCount_ == oldChild)
            {
                SetArgCount((TIntegerConstant)newChild);
            }
            else if (_semi_ == oldChild)
            {
                SetSemi((TSemi)newChild);
            }
        }
    }

}
