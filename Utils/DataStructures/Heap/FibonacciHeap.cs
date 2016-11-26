using System;
using System.Collections.Generic;
using Utils.DataStructures.Internal;
using Utils.DataStructures.Nodes;

namespace Utils.DataStructures
{
    public class FibonacciHeap<TKey, TValue>
        : HeapBase<TKey, TValue>
        where TKey : struct
        where TValue : IEquatable<TValue>
    {
        #region Fields

        internal DisseminateNode<TKey, TValue> FirstRoot;
        internal DisseminateNode<TKey, TValue> MinNode;

        private readonly NodeTraversalActions<TKey, TValue> _traversalActions;

        #endregion

        #region Genesis

        public FibonacciHeap(IComparer<TKey> keyComparer = null)
            : base(keyComparer)
        {
            _traversalActions = new NodeTraversalActions<TKey, TValue>();
        }

        #endregion

        #region HeapBase<,> overrides

        public override bool IsReadOnly { get { return false; } }

        public override ItemCollection<NodeItem> Items { get { return new ItemCollection<NodeItem>(/* Enumerable of all children */null, Count); } }


        public override void Add(TKey key, TValue value)
        {
            var newNode = new DisseminateNode<TKey, TValue>(key, value);

            if (Count == 0)
            {
                FirstRoot = newNode;
                MinNode = FirstRoot;
                return;
            }

            FirstRoot.InsertBefore(newNode);
            Count++;

            if (Comparer.Compare(key, MinNode.Key) <= 0)
                MinNode = newNode;
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
            Count += other.Count;

            if (Count == 0)
            {
                FirstRoot = other.FirstRoot;
                MinNode = other.MinNode;
                return;
            }

            FirstRoot.InsertBefore(other.FirstRoot); // Inserts the other roots after all of our roots

            if (Comparer.Compare(other.MinNode.Key, MinNode.Key) < 0)
                MinNode = other.MinNode;
        }

        #endregion

        #region Helpers

        #endregion
    }
}
