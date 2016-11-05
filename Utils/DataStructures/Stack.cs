using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
            _buffer[_head--] = default(T);
            return res;
        }

        public T Peek()
        {
            if (Count == 0)
                throw new InvalidOperationException("The Stack<T> is empty.");

            return _buffer[_head];
        }

        public T this[int idx]
        {
            get
            {
                if (idx < 0 || idx > Count)
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
            if (_head < Count)
                return;

            int newCapacity = (int)(Capacity * ReallocateFactor);
            T[] newBuffer = new T[newCapacity];
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _head * Marshal.SizeOf(typeof(T)));

            for (int i = 0; i < Count; i++)
                _buffer[i] = default(T);

            _buffer = newBuffer;
        }

        #endregion
    }
}
