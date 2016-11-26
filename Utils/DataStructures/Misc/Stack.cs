using System;

namespace Utils.DataStructures
{
    public class Stack<T>
    {
        #region Constants, fields and properties

        private const float ReallocateFactor = 2f;


        private T[] _buffer;
        private int _head;


        public int Count { get { return _head; } }
        public int Capacity { get { return _buffer.Length; } }

        #endregion

        #region Genesis

        public Stack(int initialCapacity = 16)
        {
            if (initialCapacity <= 0)
                throw new ArgumentOutOfRangeException("initialCapacity", "The capacity must not be negative.");

            _buffer = new T[initialCapacity];
        }

        #endregion

        #region Public accessors

        public void Push(T item)
        {
            _buffer[_head++] = item;
            CheckReallocate();
        }

        public T Pop()
        {
            T res = Peek();
            _buffer[--_head] = default(T);
            return res;
        }

        public T Peek()
        {
            if (Count == 0)
                throw new InvalidOperationException("The Stack<T> is empty.");

            return _buffer[_head - 1];
        }

        public T this[int idx]
        {
            get
            {
                if (idx < 0 || idx >= Count)
                    throw new ArgumentOutOfRangeException("idx", "The index was out of range of the array.");
                return _buffer[idx];
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _head; i++)
                _buffer[i] = default(T);

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
            Array.Copy(_buffer, newBuffer, _head);

            for (int i = 0; i < _head; i++)
                _buffer[i] = default(T);

            _buffer = newBuffer;
        }

        #endregion
    }
}
