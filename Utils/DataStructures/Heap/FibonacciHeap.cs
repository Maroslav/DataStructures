﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

            public int Order;

            public HeapNode(TKey key, TValue value)
                : base(key, value)
            { }

            public override string ToString()
            {
                // Debug version
                return string.Format("{0} :: {1} : {2}", Order, Key, Value);
            }
        }

        #endregion

        #region Fields

        private HeapNode _minNode;
        private HeapNode _consolidateRoots;

        internal int LastConsolidateDepth;


        // We use a stack for this because it has sufficiently convenient ops and is handcrafted (a requirement)
        private readonly Stack<HeapNode> _roots = new Stack<HeapNode>();

        private readonly NodeTraversalActions<TKey, TValue, DisseminateNode<TKey, TValue>, NodeTraversalAction> _traversalActions;

        #endregion

        #region Genesis

        public FibonacciHeap(IComparer<TKey> keyComparer = null)
            : base(keyComparer)
        {
            _traversalActions = new NodeTraversalActions<TKey, TValue, DisseminateNode<TKey, TValue>, NodeTraversalAction>();
        }

        #endregion

        #region HeapBase<,> overrides

        public override bool IsReadOnly { get { return false; } }

        public override ItemCollection<NodeItem<TKey, TValue>> Items
        {
            get
            {
                NodeItem<TKey, TValue>[] items = new NodeItem<TKey, TValue>[Count];

                if (Count == 0)
                    return new ItemCollection<NodeItem<TKey, TValue>>(items, 0);

                int i = 0;

                _traversalActions.SetActions(preAction: n =>
                {
                    items[i++] = n;
                    return true;
                });

                foreach (var root in GetRoots())
                    root.Sift(_traversalActions);

                if (_consolidateRoots != null)
                    foreach (var root in _consolidateRoots.GetSiblings().Cast<HeapNode>())
                        root.Sift(_traversalActions);

                Debug.Assert(i == Count);
                return new ItemCollection<NodeItem<TKey, TValue>>(items, Count);
            }
        }


        public override NodeItem<TKey, TValue> Add(TKey key, TValue value)
        {
            var newNode = new HeapNode(key, value);

#if VERBOSE
            Console.WriteLine("Current min ({0}) >> {1}", _roots.Count(r => r != null), PeekMin());
            Console.WriteLine();
            Console.WriteLine(">> Add >> " + newNode);
#endif

            if (Count == 0)
            {
                Debug.Assert(_roots.Count == 0);
                _minNode = newNode;
                _roots.Push(_minNode);
                Count = 1;
                return newNode;
            }

            // Check for an improved minimum
            if (Comparer.Compare(key, _minNode.Key) <= 0)
                _minNode = newNode;

            // Only store the node for a later consolidation
            AddForConsolidation(newNode);
            Count++;

            return newNode;
        }

        public override NodeItem<TKey, TValue> PeekMin()
        {
            if (Count == 0)
                return default(NodeItem<TKey, TValue>);

            return _minNode;
        }


        public override void DecreaseKey(NodeItem<TKey, TValue> node, TKey newKey)
        {
#if VERBOSE
            Console.WriteLine("Current min ({0}) >> {1}", _roots.Count(r => r != null), PeekMin());
            Console.WriteLine();
            Console.WriteLine(">> Decrease key >> {0} >> {1}", node, newKey);
#endif

            var nNode = node as HeapNode;

            if (nNode == null)
                throw new ArgumentException(
                    "The argument is invalid. Have you specified a node that is saved in this datastructure?", "node");

            int comp = Comparer.Compare(newKey, nNode.Key);

            if (comp > 0)
                throw new ArgumentException("Trying to increase the key.", "newKey");


            nNode.Key = newKey;

            // Check if we have a new minimum
            comp = Comparer.Compare(nNode.Key, _minNode.Key);

            if (comp < 0)
                _minNode = nNode;


            // Check if we validated the heap property
            if (nNode.Parent == null || Comparer.Compare(nNode.Key, nNode.Parent.Key) >= 0)
                return; // Heap property is OK


            // Heap property is invalid -- cut the node from its parent and make it one of our roots
            var parent = (HeapNode)nNode.Parent;

            nNode.IsMarked = false;
            nNode.CutFromFamily();
            Debug.Assert(nNode.LeftSibling == nNode.RightSibling && nNode.RightSibling == nNode);
            // We consolidate all the cut nodes at once later

            try
            {
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
                    nNode.InsertBefore(p); // Insert as the last (biggest) node -- parents are always at least as big as their children
                }
            }
            finally
            {
                AddForConsolidation(nNode);
            }
        }

        public override void DeleteMin()
        {
#if VERBOSE
            Console.WriteLine("Current min ({0}) >> {1}", _roots.Count(r => r != null), PeekMin());
            Console.WriteLine();
            Console.WriteLine(">> Delete-Min");
#endif

            var snapshot = Items.ToArray();
            var TMPMIN = _minNode;

            LastConsolidateDepth = 0;

            if (Count == 0)
                return;

            if (Count == 1)
            {
                Debug.Assert(_minNode.FirstChild == null);
                Debug.Assert(_minNode.LeftSibling == _minNode.RightSibling && _minNode.RightSibling == _minNode);
                _minNode.Dispose();
                _minNode = null;
                _consolidateRoots = null;
                Debug.Assert(_roots.Count == 1);
                _roots.Stretch(0);
                Count = 0;
                return;
            }

            Debug.Assert(_minNode != null);
            Debug.Assert(_minNode.LeftSibling != null && _minNode.RightSibling != null);

            // Cut the minimum from roots
            if (_roots[_minNode.Order] == _minNode)
                _roots[_minNode.Order] = null;
            else
                // MinNode must be among nodes to be consolidated
                Debug.Assert(_consolidateRoots.GetSiblings().Contains(_minNode));

            // Remove min from its list and gut it
            var children = (HeapNode)_minNode.FirstChild;

            // Remove from family -- preserves children
            _minNode.CutFromFamily();
            _minNode.Dispose();
            _minNode = null;
            Count--;


            // Make min's children into roots
            if (children != null)
            {
                Debug.Assert(children.LeftSibling != null && children.RightSibling != null);
                children.Parent = null;
                AddForConsolidation(children);
            }

            // Combine roots and find the new minimum
            Consolidate();
        }

        public override void Delete(NodeItem<TKey, TValue> node)
        {
            throw new NotImplementedException("TODO");
        }


        public override void Merge(IPriorityQueue<TKey, TValue> other)
        {
            var heap = other as FibonacciHeap<TKey, TValue>;

            if (heap == null)
                throw new ArgumentException("Can only merge with another Fibonacci Heap.", "other");

            Merge(heap);
        }

        public override void Clear()
        {
            if (Count == 0)
                return;

            _traversalActions.SetActions(postAction: n =>
            {
                n.Dispose();
                return true;
            });

            // Start Dispose from the last node (postAction)
            _consolidateRoots.Sift(_traversalActions);

            foreach (var root in GetRoots())
                root.Sift(_traversalActions);

            _roots.Clear();
            _minNode = null;
            _consolidateRoots = null;
            Count = 0;
        }

        #endregion

        #region Public methods

        public void Merge(FibonacciHeap<TKey, TValue> other)
        {
            if (other.Count == 0)
                return;

            // Loot the other heap
            foreach (var root in other.GetRoots())
                AddForConsolidation(root);

            if (other._consolidateRoots != null)
                AddForConsolidation(other._consolidateRoots);

            // We make order only during deleteMin, not here
            Count += other.Count;

            // Check for an improved min
            int comp = Comparer.Compare(other._minNode.Key, _minNode.Key);

            if (comp < 0)
                _minNode = other._minNode;
        }

        #endregion

        #region Helpers

        internal IEnumerable<HeapNode> GetRoots()
        {
            HeapNode last = null;

            foreach (var root in _roots)
            {
                if (root == null)
                    continue;

                last = root;
                yield return root;
            }

            if (Count > 0)
            {
                Debug.Assert(last != null);
                _roots.Stretch(last.Order + 1); // Shrink the roots down (no resizing)
            }
        }


        #region Node consolidation

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddForConsolidation(HeapNode nodeList)
        {
            Debug.Assert(nodeList != null);
            Debug.Assert(nodeList.LeftSibling != null && nodeList.RightSibling != null);

            nodeList.Parent = null;

            if (_consolidateRoots == null)
                _consolidateRoots = nodeList;
            else
                _consolidateRoots.InsertBefore(nodeList);
        }

        private void Consolidate()
        {
            try
            {
                if (_consolidateRoots == null)
                    return;

                // Go through our messy nodes and add them one by one into our roots.
                // Solve any chain of carry bits right away. This does not increase the
                // complexity wrt. adding of sorted lists. We need to do the work anyway.
                foreach (var consolidateNode in _consolidateRoots.GetSiblings().Cast<HeapNode>())
                {
                    HeapNode carry = AddNode(consolidateNode);

                    while (carry != null)
                        carry = AddNode(carry);
                }

                _consolidateRoots = null;
            }
            finally
            {
                // We have to find the new minNode...
                var roots = GetRoots();

                _minNode = roots.First();

                foreach (var root in roots.Skip(1)) // There was at least something to consolidate (count>0)
                    if (Comparer.Compare(root.Key, _minNode.Key) < 0)
                        _minNode = root;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HeapNode AddNode(HeapNode add)
        {
            Debug.Assert(add != null);

            // Do not keep links between root nodes (they are harder to maintain)
            add.Parent = null;
            add.LeftSibling = add;

            // Assert enough capacity for our roots -- force resize
            if (_roots.Count <= add.Order)
                _roots.Stretch(add.Order + 1);

            // Insert Add to our roots
            HeapNode first = _roots[add.Order];

            if (first == null)
            {
                // Directly insert Add to its place
                _roots[add.Order] = add;
                return null; // There is no carry
            }

            // Return the combination as a carry that will be propagated to a higher tier
            _roots[add.Order] = null;
            return CombineNodes(first, add);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HeapNode CombineNodes(HeapNode first, HeapNode add)
        {
            int comp = Comparer.Compare(first.Key, add.Key);

            HeapNode smaller = first;
            HeapNode other = add;

            if (comp > 0)
                Swap(ref smaller, ref other);

            Debug.Assert(smaller.Order == other.Order);
            other.Cut();
            smaller.AddChild(other);
            smaller.Order++;
            LastConsolidateDepth++;

            Debug.Assert(smaller.ChildrenCount <= smaller.Order);

#if VERBOSE
            Console.WriteLine("Merging tree under another: {0} (under {1})", other, smaller);

            Console.WriteLine("All siblings ({0}): ", smaller.ChildrenCount);
            foreach (var siblingNode in smaller.FirstChild.GetSiblings().Take(4))
                Console.WriteLine(siblingNode);
#endif

            return smaller;
        }

        #endregion

        #endregion
    }
}
