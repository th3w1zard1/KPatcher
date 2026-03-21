// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections;
using System.Collections.Generic;

using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
    /// <summary>
    /// Linked list that enforces parent/ownership rules via an <see cref="ICast{T}"/> hook on inserts.
    /// </summary>
    public sealed class TypedLinkedList<T> : IEnumerable<T>
    {
        private readonly LinkedList<T> _list = new LinkedList<T>();
        private readonly ICast<T> _cast;

        public TypedLinkedList()
        {
            _cast = NoCast<T>.Instance;
        }

        public TypedLinkedList(IEnumerable<T> c)
            : this()
        {
            AddAll(c);
        }

        public TypedLinkedList(ICast<T> cast)
        {
            _cast = cast ?? throw new ArgumentNullException(nameof(cast));
        }

        public TypedLinkedList(IEnumerable<T> c, ICast<T> cast)
            : this(cast)
        {
            AddAll(c);
        }

        public ICast<T> GetCast()
        {
            return _cast;
        }

        public int Count
        {
            get { return _list.Count; }
        }

        public void Clear()
        {
            _list.Clear();
        }

        public void AddFirst(T item)
        {
            _list.AddFirst(_cast.CastObject(item));
        }

        public void AddLast(T item)
        {
            _list.AddLast(_cast.CastObject(item));
        }

        public bool Remove(T item)
        {
            return _list.Remove(item);
        }

        public void AddAll(IEnumerable<T> c)
        {
            foreach (T x in c)
            {
                AddLast(x);
            }
        }

        /// <summary>
        /// Replaces <paramref name="oldChild"/> with a casted <paramref name="newChild"/> at the same position, or removes <paramref name="oldChild"/> when <paramref name="newChild"/> is null (SableCC list-iterator semantics).
        /// </summary>
        public void ReplaceChild(T oldChild, T newChild)
        {
            LinkedListNode<T> node = _list.Find(oldChild);
            if (node == null)
            {
                return;
            }

            if (newChild != null)
            {
                T casted = _cast.CastObject(newChild);
                if (oldChild is Node oc)
                {
                    oc.SetParent(null);
                }

                _list.AddBefore(node, casted);
                _list.Remove(node);
            }
            else
            {
                if (oldChild is Node oc)
                {
                    oc.SetParent(null);
                }

                _list.Remove(node);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Snapshot for indexed access (SableCC Java <c>LinkedList</c> compatibility).
        /// </summary>
        public List<T> ToList()
        {
            var r = new List<T>(_list.Count);
            foreach (T x in _list)
            {
                r.Add(x);
            }

            return r;
        }

        /// <summary>
        /// Last element in list order, or default when empty.
        /// </summary>
        public T GetLast()
        {
            if (_list.Last == null)
            {
                return default(T);
            }

            return _list.Last.Value;
        }

        public T FirstOrDefault()
        {
            foreach (T x in _list)
            {
                return x;
            }

            return default(T);
        }
    }
}
