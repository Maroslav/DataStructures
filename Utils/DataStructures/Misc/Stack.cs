using System;
using System.Collections;
using System.Collections.Generic;

namespace Utils.DataStructures
{
    public class Stack<T>
        : IEnumerable<T>
    {
        #region Constants, fields and properties

        private const float ReallocateFactor = 2f;


        internal T[] Buffer;
        private int _head;


        public int Count { get { return _head; } }
        public int Capacity { get { return Buffer.Length; } }

        #endregion

        #region Genesis

        public Stack(int initialCapacity = 16)
        {
            if (initialCapacity <= 0)
                throw new ArgumentOutOfRangeException("initialCapacity", "The capacity must not be negative.");

            Buffer = new T[initialCapacity];
        }

        #endregion

        #region Public accessors

        public void Push(T item)
        {
            Buffer[_head++] = item;
            CheckReallocate();
        }

        public T Pop()
        {
            T res = Peek();
            Buffer[--_head] = default(T);
            return res;
        }

        public T Peek()
        {
            if (Count == 0)
                throw new InvalidOperationException("The Stack<T> is empty.");

            return Buffer[_head - 1];
        }

        public T this[int idx]
        {
            get
            {
                if (idx < 0 || idx >= Count)
                    throw new ArgumentOutOfRangeException("idx", "The index was out of range of the array.");
                return Buffer[idx];
            }

            internal set
            {
                if (idx < 0 || idx >= Count)
                    throw new ArgumentOutOfRangeException("idx", "The index was out of range of the array.");
                Buffer[idx] = value;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _head; i++)
                Buffer[i] = default(T);

            _head = 0;
        }

        #endregion

        #region Enumeration

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return Buffer[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Helpers

        private void CheckReallocate()
        {
            if (_head < Capacity)
                return;

            int newCapacity = Math.Max(_head, (int)(Capacity * ReallocateFactor));
            T[] newBuffer = new T[newCapacity];
            Array.Copy(Buffer, newBuffer, _head);

            for (int i = 0; i < _head; i++)
                Buffer[i] = default(T);

            Buffer = newBuffer;
        }

        /// <summary>
        /// Sets head to capacity.
        /// If larger than Count, pushes capacity-Count nulls; reallocates if neccessary.
        /// This enables directly indexing items up to capacity.
        /// </summary>
        /// <param name="capacity">The new capacity and item cound.</param>
        internal void Stretch(int capacity)
        {
            _head = capacity;
            CheckReallocate();
        }

        #endregion
    }
}
