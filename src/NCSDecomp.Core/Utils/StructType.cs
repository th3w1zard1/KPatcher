// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Composite type for structs (and vectors as a special case). Port of DeNCS StructType.java.
    /// </summary>
    public class StructType : DecompType
    {
        private List<DecompType> _types = new List<DecompType>();
        private bool _allTyped = true;
        private string _typeName;
        private List<string> _elements;

        public StructType()
            : base(VtStruct)
        {
            _size = 0;
        }

        public override void Close()
        {
            if (_types != null)
            {
                for (int i = 0; i < _types.Count; i++)
                {
                    _types[i].Close();
                }

                _types = null;
            }

            _elements = null;
        }

        public void AddType(DecompType type)
        {
            _types.Add(type);
            if (type.Equals(VtInvalid))
            {
                _allTyped = false;
            }

            _size = _size + type.Size();
        }

        public void AddTypeStackOrder(DecompType type)
        {
            _types.Insert(0, type);
            if (type.Equals(VtInvalid))
            {
                _allTyped = false;
            }

            _size = _size + type.Size();
        }

        public bool IsVector()
        {
            if (_size != 3)
            {
                return false;
            }

            for (int i = 0; i < 3; i++)
            {
                if (!_types[i].Equals(VtFloat))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool IsTyped()
        {
            return _allTyped;
        }

        public void UpdateType(int pos, DecompType type)
        {
            _types[pos] = type;
            UpdateTyped();
        }

        public List<DecompType> Types()
        {
            return _types;
        }

        protected void UpdateTyped()
        {
            _allTyped = true;
            for (int i = 0; i < _types.Count; i++)
            {
                if (!_types[i].IsTyped())
                {
                    _allTyped = false;
                    return;
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            StructType other = obj as StructType;
            if (other == null || _types == null || other._types == null || _types.Count != other._types.Count)
            {
                return false;
            }

            for (int i = 0; i < _types.Count; i++)
            {
                if (!_types[i].Equals(other._types[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            if (_types == null)
            {
                return 0;
            }

            unchecked
            {
                int h = 17;
                foreach (DecompType t in _types)
                {
                    h = h * 31 + (t != null ? t.GetHashCode() : 0);
                }

                return h;
            }
        }

        public void TypeName(string name)
        {
            _typeName = name;
        }

        public string TypeName()
        {
            return _typeName;
        }

        public override string ToDeclString()
        {
            return IsVector() ? ToString(VtVector) : ToString() + " " + _typeName;
        }

        public string ElementName(int i)
        {
            if (_elements == null)
            {
                SetElementNames();
            }

            return _elements[i];
        }

        public override DecompType GetElement(int pos)
        {
            int remaining = pos;
            foreach (DecompType entry in _types)
            {
                int sz = entry.Size();
                if (remaining <= sz)
                {
                    return entry.GetElement(remaining);
                }

                remaining -= sz;
            }

            throw new InvalidOperationException("Pos was greater than struct size");
        }

        private void SetElementNames()
        {
            _elements = new List<string>();
            var typecounts = new Dictionary<DecompType, int>();
            if (IsVector())
            {
                _elements.Add("x");
                _elements.Add("y");
                _elements.Add("z");
            }
            else
            {
                for (int i = 0; i < _types.Count; i++)
                {
                    DecompType t = _types[i];
                    int count;
                    if (typecounts.TryGetValue(t, out int tc))
                    {
                        count = 1 + tc;
                    }
                    else
                    {
                        count = 1;
                    }

                    _elements.Add(t.ToString() + count);
                    typecounts[t] = count + 1;
                }
            }
        }
    }
}
