using System;
using System.Collections.Generic;
using Utils.DataStructures.Nodes;

namespace Utils.DataStructures
{
    public class FibonacciHeap<TKey, TValue>
        : HeapBase<TKey, TValue>
        where TKey : struct
        where TValue : IEquatable<TValue>
    {
        #region Fields

        internal DisseminateNode<TKey, TValue> RootsParent;
        internal DisseminateNode<TKey, TValue> MinNode;

        #endregion

        #region Genesis

        public FibonacciHeap(IComparer<TKey> comparer = null)
            : base(comparer)
        { }

        #endregion

        #region HeapBase<,> overrides

        public override bool IsReadOnly { get { return false; } }

        public override ItemCollection<NodeItem> Items { get { return new ItemCollection<NodeItem>(/* Enumerable of all children */null, Count); } }


        public override void Add(TKey key, TValue value)
        {
            RootsParent.AddChild(new DisseminateNode<TKey, TValue>(key, value));
        }

        public override NodeItem PeekMin()
        {
            if (Count == 0)
                return default(NodeItem);

            return MinNode;
        }

        public override NodeItem DeleteMin()
        {
            NodeItem min = PeekMin();



            Count--;
            return min;
        }

        public override void Clear()
        {
        }

        #endregion

        #region Public methods

        public void Merge(FibonacciHeap<TKey, TValue> other)
        {

        }

        #endregion

        #region Helpers
        
        #endregion
    }
}
