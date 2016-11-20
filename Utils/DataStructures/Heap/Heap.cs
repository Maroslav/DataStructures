using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Utils.DataStructures
{
    public class Heap<TKey, TValue>
        : HeapBase<TKey, TValue>
    {
        protected readonly NodeItem[] m_heap;


        protected int MinIndex { get { return 1; } }
        protected int MaxIndex { get { return Count + 1; } }

        public int Capacity { get { return m_heap.Length - 1; } }


        public Heap(int capacity, IComparer<TKey> comparer = null)
            : base(comparer)
        {
            Debug.Assert(capacity > 0);
            // Heaps ought to be indexed from 1
            m_heap = new NodeItem[capacity + 1];
        }


        #region HeapBase<,> overrides

        public override bool IsReadOnly { get { return false; } }

        public override ItemCollection<NodeItem> Items { get { return new ItemCollection<NodeItem>(m_heap.Skip(1), Count); } }

        public override void Add(TKey key, TValue value)
        {
            // TODO: check reallocate
            m_heap[MaxIndex] = new NodeItem(key, value);


            int currentIdx = MaxIndex;
            int lastIdx = 0;

            while (currentIdx != lastIdx)
            {
                lastIdx = currentIdx;
                currentIdx = Heapify(currentIdx);
            }
        }

        public override NodeItem PeekMin()
        {
            if (Count == 0)
                return default(NodeItem);

            return m_heap[MinIndex];
        }

        public override NodeItem DeleteMin()
        {
            NodeItem min = PeekMin();

            // Place the last element in place of the root element
            m_heap[MinIndex] = m_heap[MaxIndex];

            // TODO!
            //Heapify();

            Count--;
            return min;
        }

        public override void Clear()
        {
            for (int i = 0; i < m_heap.Length; i++)
            {
                m_heap[i].Dispose();
                m_heap[i] = null;
            }

            Count = 0;
        }


        #endregion

        #region Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Bubble(int idx)
        {
            // Bubble the new root through the heap
            int currentIdx = idx;
            int leftIdx = currentIdx * 2;
            int rightIdx = leftIdx + 1;
            NodeItem act = m_heap[currentIdx];
            NodeItem left = m_heap[leftIdx];
            NodeItem right = m_heap[rightIdx];

            int swapIdx = currentIdx;

            if (leftIdx <= MaxIndex && Comparer.Compare(act.Key, left.Key) <= 0)
                swapIdx = leftIdx;
            if (rightIdx <= MaxIndex && Comparer.Compare(act.Key, right.Key) <= 0)
                swapIdx = rightIdx;

            if (currentIdx == swapIdx)
                return currentIdx;

            Swap(ref m_heap[currentIdx], ref m_heap[swapIdx]);
            return swapIdx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap<T>(ref T one, ref T two)
        {
            T tmp = one;
            one = two;
            two = tmp;
        }

        #endregion
    }
}
