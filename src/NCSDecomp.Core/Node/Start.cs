// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    public sealed class Start : Node
    {
        private PProgram _pProgram_;
        private EOF _eof_;

        public Start()
        {
        }

        public Start(PProgram pProgram, EOF eof)
        {
            SetPProgram(pProgram);
            SetEOF(eof);
        }

        public override object Clone()
        {
            PProgram clonedPProgram = _pProgram_ != null ? (PProgram)_pProgram_.Clone() : null;
            EOF clonedEOF = _eof_ != null ? (EOF)_eof_.Clone() : null;
            return (object)new Start(clonedPProgram, clonedEOF);
        }

        public override void Apply(Switch sw)
        {
            ((IAnalysis)sw).CaseStart(this);
        }

        public PProgram GetPProgram()
        {
            return _pProgram_;
        }

        public void SetPProgram(PProgram node)
        {
            if (_pProgram_ != null)
            {
                _pProgram_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _pProgram_ = node;
        }

        public EOF GetEOF()
        {
            return _eof_;
        }

        public void SetEOF(EOF node)
        {
            if (_eof_ != null)
            {
                _eof_.SetParent(null);
            }

            if (node != null)
            {
                if (node.Parent() != null)
                {
                    node.Parent().RemoveChild(node);
                }

                node.SetParent(this);
            }

            _eof_ = node;
        }

        internal override void RemoveChild(Node child)
        {
            if (_pProgram_ == child)
            {
                _pProgram_ = null;
            }
            else if (_eof_ == child)
            {
                _eof_ = null;
            }
        }

        internal override void ReplaceChild(Node oldChild, Node newChild)
        {
            if (_pProgram_ == oldChild)
            {
                SetPProgram((PProgram)newChild);
            }
            else if (_eof_ == oldChild)
            {
                SetEOF((EOF)newChild);
            }
        }

        public override string ToString()
        {
            return ToString(_pProgram_) + ToString(_eof_);
        }
    }
}
