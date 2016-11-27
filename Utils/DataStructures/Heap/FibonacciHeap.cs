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
        #region Nested classes

        internal class HeapNode
            : DisseminateNode<TKey, TValue>
        {
            internal bool IsMarked { get; set; }

            public HeapNode(TKey key, TValue value)
                : base(key, value)
            { }
        }

        #endregion

        #region Fields

        internal HeapNode FirstRoot;
        internal HeapNode MinNode;

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

        public override ItemCollection<NodeItem<TKey, TValue>> Items { get { return new ItemCollection<NodeItem<TKey, TValue>>(/* Enumerable of all children */null, Count); } }


        public override NodeItem<TKey, TValue> Add(TKey key, TValue value)
        {
            var newNode = new HeapNode(key, value);

            if (Count == 0)
            {
                FirstRoot = newNode;
                MinNode = FirstRoot;
                return newNode;
            }

            FirstRoot.InsertBefore(newNode);
            Count++;

            if (Comparer.Compare(key, MinNode.Key) <= 0)
                MinNode = newNode;

            return newNode;
        }

        public override NodeItem<TKey, TValue> PeekMin()
        {
            if (Count == 0)
                return default(NodeItem<TKey, TValue>);

            return MinNode;
        }

        public override void DecreaseKey(NodeItem<TKey, TValue> node, TKey newKey)
        {
            var nNode = node as HeapNode;

            if (nNode == null)
                throw new ArgumentException(
                    "The argument is invalid. Have you specified a node that is saved in this datastructure?", "node");

            int comp = Comparer.Compare(newKey, nNode.Key);

            if (comp > 0)
                throw new ArgumentException("Trying to increase the key.", "newKey");


            nNode.Key = newKey;

            // Check if we have a new minimum
            comp = Comparer.Compare(nNode.Key, MinNode.Key);

            if (comp < 0)
                MinNode = nNode;


            // Check if we validated the heap property
            comp = Comparer.Compare(nNode.Key, nNode.Parent.Key);

            if (nNode.Parent == null || comp >= 0)
                return; // Heap property is OK


            var parent = (HeapNode)nNode.Parent;

            // Heap property is invalid -- cut the node from its parent and make it one of our roots
            nNode.IsMarked = false;
            nNode.CutFromFamily();
            FirstRoot.InsertBefore(nNode);

            if (parent == null)
                return;


            // Recursively mark and cut parents, end at root
            while (parent.Parent != null)
            {
                if (!parent.IsMarked)
                {
                    parent.IsMarked = true;
                    break;
                }

                // The parent is marked -- unmark and cut it
                var p = parent;
                parent = (HeapNode)parent.Parent;

                p.IsMarked = false;
                p.CutFromFamily();
                FirstRoot.InsertBefore(p);
            }
        }

        public override void DeleteMin()
        {
            if (Count == 0)
                return;

            Delete(MinNode);
        }

        public override void Delete(NodeItem<TKey, TValue> node)
        {
        }

        public override void Merge(IPriorityQueue<TKey, TValue> other)
        {
            var heap = other as FibonacciHeap<TKey, TValue>;

            if (heap == null)
                throw new ArgumentException("Can only merge with another Fibonacci Heap.", "other");

            Merge(heap);
        }

        public override void Clear()
        { }

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
