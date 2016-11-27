using System;

namespace Utils.DataStructures
{
    public class Stack<T>
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

        #region Helpers

        private void CheckReallocate()
        {
            if (_head < Capacity)
                return;

            int newCapacity = (int)(Capacity * ReallocateFactor);
            T[] newBuffer = new T[newCapacity];
            Array.Copy(Buffer, newBuffer, _head);

            for (int i = 0; i < _head; i++)
                Buffer[i] = default(T);

            Buffer = newBuffer;
        }

        #endregion
    }
}
