// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    /// <summary>
    /// SableCC node representing the program root; contains subroutines and optional prolog.
    /// </summary>
    public sealed class AProgram : PProgram
    {
        private PSize _size_;
        private PRsaddCommand _conditional_;
        private PJumpToSubroutine _jumpToSubroutine_;
        private PReturn _return_;
        private readonly TypedLinkedList<PSubroutine> _subroutine_;

        public AProgram()
        {
            _subroutine_ = new TypedLinkedList<PSubroutine>(new SubroutineCast(this));
        }

        public AProgram(PSize size, PRsaddCommand conditional, PJumpToSubroutine jumpToSubroutine, PReturn @return, IEnumerable<PSubroutine> subroutine)
            : this()
        {
            SetSize(size);
            SetConditional(conditional);
            SetJumpToSubroutine(jumpToSubroutine);
            SetReturn(@return);
            _subroutine_.Clear();
            _subroutine_.AddAll(subroutine);
        }

        public AProgram(PSize size, PRsaddCommand conditional, PJumpToSubroutine jumpToSubroutine, PReturn @return, XPSubroutine subroutine)
            : this()
        {
            SetSize(size);
            SetConditional(conditional);
            SetJumpToSubroutine(jumpToSubroutine);
            SetReturn(@return);
            if (subroutine != null)
            {
                while (subroutine is X1PSubroutine x1)
                {
                    _subroutine_.AddFirst(x1.GetPSubroutine());
                    subroutine = x1.GetXPSubroutine();
                }

                _subroutine_.AddFirst(((X2PSubroutine)subroutine).GetPSubroutine());
            }
        }

        public override object Clone()
        {
            PSize clonedSize = _size_ != null ? (PSize)_size_.Clone() : null;
            PRsaddCommand clonedConditional = _conditional_ != null ? (PRsaddCommand)_conditional_.Clone() : null;
            PJumpToSubroutine clonedJumpToSubroutine = _jumpToSubroutine_ != null ? (PJumpToSubroutine)_jumpToSubroutine_.Clone() : null;
            PReturn clonedReturn = _return_ != null ? (PReturn)_return_.Clone() : null;
            return (object)new AProgram(
                clonedSize,
                clonedConditional,
                clonedJumpToSubroutine,
                clonedReturn,
                CloneTypedList(_subroutine_));
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseAProgram(this);
        }

        public PSize GetSize()
        {
            return _size_;
        }

        public void SetSize(PSize node)
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

        public PRsaddCommand GetConditional()
        {
            return _conditional_;
        }

        public void SetConditional(PRsaddCommand node)
        {
            if (_conditional_ != null)
            {
                _conditional_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _conditional_ = node;
        }

        public PJumpToSubroutine GetJumpToSubroutine()
        {
            return _jumpToSubroutine_;
        }

        public void SetJumpToSubroutine(PJumpToSubroutine node)
        {
            if (_jumpToSubroutine_ != null)
            {
                _jumpToSubroutine_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _jumpToSubroutine_ = node;
        }

        public PReturn GetReturn()
        {
            return _return_;
        }

        public void SetReturn(PReturn node)
        {
            if (_return_ != null)
            {
                _return_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _return_ = node;
        }

        public TypedLinkedList<PSubroutine> GetSubroutine()
        {
            return _subroutine_;
        }

        public void SetSubroutine(IEnumerable<PSubroutine> list)
        {
            _subroutine_.Clear();
            _subroutine_.AddAll(list);
        }

        public override string ToString()
        {
            return ToString(_size_)
                + ToString(_conditional_)
                + ToString(_jumpToSubroutine_)
                + ToString(_return_)
                + ToStringEnumerable(_subroutine_);
        }

        internal override void RemoveChild(Node child)
        {
            if (_size_ == child)
            {
                _size_ = null;
            }
            else if (_conditional_ == child)
            {
                _conditional_ = null;
            }
            else if (_jumpToSubroutine_ == child)
            {
                _jumpToSubroutine_ = null;
            }
            else if (_return_ == child)
            {
                _return_ = null;
            }
            else
            {
                _subroutine_.Remove((PSubroutine)child);
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_size_ == oldChild)
            {
                SetSize((PSize)newChild);
            }
            else if (_conditional_ == oldChild)
            {
                SetConditional((PRsaddCommand)newChild);
            }
            else if (_jumpToSubroutine_ == oldChild)
            {
                SetJumpToSubroutine((PJumpToSubroutine)newChild);
            }
            else if (_return_ == oldChild)
            {
                SetReturn((PReturn)newChild);
            }
            else
            {
                _subroutine_.ReplaceChild((PSubroutine)oldChild, (PSubroutine)newChild);
            }
        }

        private sealed class SubroutineCast : ICast<PSubroutine>
        {
            private readonly AProgram _outer;

            public SubroutineCast(AProgram outer)
            {
                _outer = outer;
            }

            public PSubroutine CastObject(object o)
            {
                if (!(o is PSubroutine node))
                {
                    throw new System.InvalidCastException("Expected PSubroutine but got: " + (o != null ? o.GetType().FullName : "null"));
                }

                if (node.Parent() != null && node.Parent() != _outer)
                {
                    node.Parent().RemoveChild(node);
                }

                if (node.Parent() == null || node.Parent() != _outer)
                {
                    node.SetParent(_outer);
                }

                return node;
            }
        }
    }
}
