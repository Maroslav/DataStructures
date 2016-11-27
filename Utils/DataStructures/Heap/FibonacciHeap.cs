﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            public HeapNode(TKey key, TValue value)
                : base(key, value)
            { }
        }

        #endregion

        #region Fields

        internal HeapNode FirstRoot;
        internal HeapNode MinNode;

        // We use a stack for this because it has sufficiently convenient ops and is handcrafted (a requirement)
        private readonly Stack<HeapNode> _roots = new Stack<HeapNode>();

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
            Consolidate(other.FirstRoot, other.Count);
        }

        #endregion

        #region Helpers

        [Flags]
        private enum Bits
        {
            First = 1,
            Add = 2,
            Carry = 4,
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Consolidate(HeapNode firstNode, int count, bool checkMin = false)
        {
            Debug.Assert(firstNode != null);

            // Nodes should be ordered from the smallest -- this makes it the same as binary digit addition
            HeapNode carry = null;
            int currentOrder = 0;

            // We end when we reach the end of the list and when there is no leftover carry
            while (firstNode != null || carry != null)
            {
                // Set the order for the current iteration
                if (carry != null)
                    currentOrder++; // The carry propagates only to the next order
                else if (firstNode.ChildrenCount > currentOrder)
                    currentOrder = firstNode.ChildrenCount; // If there is no carry, we handle the next node in the list

                // Assert root array size
                if (_roots.Count == currentOrder)
                    _roots.Push(null); // Forces resize; will be replaced by the joined node

                // Setup the not-null flag -- inputs
                HeapNode add = null;
                Bits inputs = 0;

                if (_roots[currentOrder] != null)
                    inputs |= Bits.First;

                if (firstNode != null && firstNode.ChildrenCount == currentOrder)
                {
                    // Set the Add node for this iteration -- it is only valid if it is of the current order
                    add = firstNode;
                    inputs |= Bits.Add;

                    // Update firstNode for the next iteration; we work with add in this iteration
                    firstNode = (HeapNode)firstNode.RightSibling;

                    if (firstNode != add)
                        firstNode.CutFromFamily(); // Remove it from the list
                    else
                        firstNode = null; // This is the only node left in the list -- set it to null to signal exit
                }

                if (carry != null)
                    inputs |= Bits.Carry;

                // Join the nodes together, update the roots and minNode and create the new carry
                AddNodes(ref _roots.Buffer[currentOrder], add, ref carry, inputs, checkMin);
            }

            Count += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddNodes(ref HeapNode first, HeapNode add, ref HeapNode carry, Bits inputs, bool checkMin = false)
        {
            Debug.Assert(inputs > 0);

            switch (inputs)
            {
                case Bits.First | Bits.Add:
                case Bits.First | Bits.Add | Bits.Carry:
                    var tmp = first;
                    first = carry;
                    carry = Join(tmp, add, checkMin);
                    return;

                case Bits.First | Bits.Carry:
                    first = Join(first, carry, checkMin);
                    carry = null;
                    return;
                case Bits.Add | Bits.Carry:
                    first = Join(add, carry, checkMin);
                    carry = null;
                    return;

                case Bits.First:
                    return;
                case Bits.Add:
                    first = add;
                    return;
                case Bits.Carry:
                    first = carry;
                    carry = null;
                    return;

                default:
                    throw new ArgumentOutOfRangeException("inputs", inputs, "This should not happen..");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HeapNode Join(HeapNode first, HeapNode add, bool checkMin = false)
        {
            int comp = Comparer.Compare(first.Key, add.Key);

            HeapNode smaller = first;
            HeapNode other = add;

            if (comp > 0)
                Swap(ref smaller, ref other);

            smaller.AddChild(other);

            if (checkMin && Comparer.Compare(smaller.Key, MinNode.Key) < 0)
                MinNode = smaller;

            return smaller;
        }

        #endregion
    }
}
