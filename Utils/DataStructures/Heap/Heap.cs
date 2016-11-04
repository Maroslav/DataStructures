using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils.DataStructures
{
    public class Heap<T>
    {
        private readonly IComparer<T> m_comparer;

        protected readonly T[] m_heap;


        protected int MinIndex { get { return 1; } }
        protected int MaxIndex { get { return Count + 1; } }

        public int Count { get; protected set; }
        public int Capacity { get { return m_heap.Length - 1; } }


        public Heap(int capacity, IComparer<T> comparer = null)
        {
            m_comparer = comparer ?? Comparer<T>.Default;

            Debug.Assert(capacity > 0);
            // Heaps ought to be indexed from 1
            m_heap = new T[capacity + 1];
        }


        public T PeekMin()
        {
            if (Count == 0)
                return default(T);

            return m_heap[MinIndex];
        }

        public T PopMin()
        {
            T min = PeekMin();
            DeleteMin();
            return min;
        }

        private void DeleteMin()
        {
            // Place the last element in place of the root element
            m_heap[MinIndex] = m_heap[MaxIndex];

            // Bubble the new root through the heap
            int currentIdx = MinIndex;
            int leftIdx = currentIdx*2;
            int rightIdx = leftIdx + 1;
            T act = m_heap[currentIdx];
            T left = m_heap[leftIdx];
            T right = m_heap[rightIdx];

            int swapIdx = currentIdx;

            if (leftIdx <= MaxIndex && m_comparer.Compare(act, left) <= 0)
                swapIdx = leftIdx;
            if (rightIdx <= MaxIndex && m_comparer.Compare(act, right) <= 0)
                swapIdx = rightIdx;

            if (swapIdx != currentIdx)
                currentIdx = swapIdx;
        }

        public void Add(T value)
        {

        }
    }
}
