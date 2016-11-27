using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Utils.DataStructures.Nodes;

namespace Utils.DataStructures
{
    public class Heap<TKey, TValue>
        : HeapBase<TKey, TValue>
    {
        protected readonly NodeItem<TKey, TValue>[] m_heap;


        protected int MinIndex { get { return 1; } }
        protected int MaxIndex { get { return Count + 1; } }

        public int Capacity { get { return m_heap.Length - 1; } }


        public Heap(int capacity, IComparer<TKey> keyComparer = null)
            : base(keyComparer)
        {
            Debug.Assert(capacity > 0);
            // Heaps ought to be indexed from 1
            m_heap = new NodeItem<TKey, TValue>[capacity + 1];
        }


        #region HeapBase<,> overrides

        public override bool IsReadOnly { get { return false; } }

        public override ItemCollection<NodeItem<TKey, TValue>> Items { get { return new ItemCollection<NodeItem<TKey, TValue>>(m_heap.Skip(1), Count); } }


        public override NodeItem<TKey, TValue> Add(TKey key, TValue value)
        {
            // TODO: check reallocate
            var newNode = new NodeItem<TKey, TValue>(key, value);
            m_heap[MaxIndex] = newNode;


            int currentIdx = MaxIndex;
            int lastIdx = 0;

            while (currentIdx != lastIdx)
            {
                lastIdx = currentIdx;
                currentIdx = Heapify(currentIdx);
            }

            return newNode;
        }


        public override NodeItem<TKey, TValue> PeekMin()
        {
            if (Count == 0)
                return default(NodeItem<TKey, TValue>);

            return m_heap[MinIndex];
        }


        public override void DecreaseKey(NodeItem<TKey, TValue> node, TKey newKey)
        {
            // TODO
        }

        public override void DeleteMin()
        {
            // Place the last element in place of the root element
            m_heap[MinIndex] = m_heap[MaxIndex];

            // TODO!
            //Heapify();

            Count--;
        }

        public override void Delete(NodeItem<TKey, TValue> node)
        {
            // TODO: store idx in the node?
        }


        public override void Merge(IPriorityQueue<TKey, TValue> other)
        {
            // TODO
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

        private int Heapify(int idx)
        {
            // TODO: exchange with parents while it is larger than the parent
            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Bubble(int idx)
        {
            // Bubble the new root through the heap
            int currentIdx = idx;
            int leftIdx = currentIdx * 2;
            int rightIdx = leftIdx + 1;
            NodeItem<TKey, TValue> act = m_heap[currentIdx];
            NodeItem<TKey, TValue> left = m_heap[leftIdx];
            NodeItem<TKey, TValue> right = m_heap[rightIdx];

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
