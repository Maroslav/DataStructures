﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Utils.DataStructures.Internal;

namespace Utils.DataStructures
{
    public class SplayTree<TKey, TValue>
        : DictionaryBase<TKey, TValue>
        where TKey : struct
        where TValue : IEquatable<TValue>
    {
        #region Fields

        internal Node<TKey, TValue> Root;

        // Local variable to reduce stack load during recursion (we assume single-threaded usage)
        private readonly NodeTraversalActions<TKey, TValue> _traversalActions;

        #endregion

        #region Genesis

        public SplayTree(IComparer<TKey> keyComparer = null)
        {
            _traversalActions = new NodeTraversalActions<TKey, TValue>(keyComparer);
        }

        #endregion

        #region Enumeration

        public override ItemCollection<NodeItem> Items
        {
            get
            {
                NodeItem[] items = new NodeItem[Count];

                if (Count == 0)
                    return new ItemCollection<NodeItem>(items, 0);

                int i = 0;

                _traversalActions.SetActions(inAction: n =>
                {
                    items[i++] = n;
                    return true;
                });

                Root.SiftLeft(_traversalActions);

                return new ItemCollection<NodeItem>(items, Count);
            }
        }

        public override string ToString()
        {
            return ":: " + Count + " ::\n" + (Root != null ? Root.ToString() : "Empty");
        }

        #endregion

        #region IDictionary<> overrides

        public override int Count { get; protected set; }
        public override bool IsReadOnly { get { return false; } }


        public override void Add(TKey key, TValue value)
        {
            // 1. Find the parent of the place in the tree, where the key should be inserted
            Node<TKey, TValue> near = FindNear(key);

            // If the tree is empty, insert new root
            if (near == null)
            {
                Debug.Assert(Count == 0);
                Root = new Node<TKey, TValue>(key, value);
                Count++;
                return;
            }

            // 2. Insert the key/value
            int comp = _traversalActions.KeyComparer.Compare(key, near.Key);

            // If we found an exact key match, just alter the node's value
            if (comp == 0)
            {
                near.Value = value;
                return;
            }

            // The key is not present in the tree, create a new node for it
            Node<TKey, TValue> newNode = new Node<TKey, TValue>(key, value);

            if (comp < 0)
            {
                Debug.Assert(near.LeftChild == null);
                near.LeftChild = newNode;
            }
            else
            {
                Debug.Assert(near.RightChild == null);
                near.RightChild = newNode;
            }

            Count++;

            // 3. Splay the newly inserted node to the root
            newNode.Splay(out Root);
        }

        public override bool Remove(TKey key)
        {
            if (!Splay(key))
                return false;

            // Root is now the node to be removed
            Debug.Assert(Root != null);

            Node<TKey, TValue> leftTree = Root.LeftChild;

            // 1. If the root's left subtree is empty, the root will start with the right subtree
            if (leftTree == null)
            {
                Node<TKey, TValue> oldRoot = Root;
                Root = oldRoot.RightChild;
                if (Root != null)
                    Root.Parent = null;
                oldRoot.Dispose();
                Count--;
                return true;
            }

            // 2. Find the right-most node in the root's left subtree -- it will become the new root
            Node<TKey, TValue> rightMost = null;

            _traversalActions.SetActions(inAction: n =>
            {
                rightMost = n;
                return false; // Terminate the DFS when we find the first node
            });

            leftTree.SiftRight(_traversalActions);
            Debug.Assert(rightMost != null); // Count > 0: there should be at least the root

            // 3. Splay the right-most node
            // Remove the parent of root's left child to not splay up to root
            leftTree.Parent = null;
            rightMost.Splay(out Root);

            // 4. Right-most is now root of the left tree (and has no right subtree); merge it with Root
            leftTree = rightMost;
            Debug.Assert(leftTree.RightChild == null); // Splay on the right-most node should make it have no right (larger) children

            leftTree.RightChild = Root.RightChild;
            if (leftTree.RightChild != null)
                leftTree.RightChild.Parent = leftTree;

            Root.Clear();
            Root = leftTree;
            Count--;

            return true;
        }


        public override bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);

            if (!Splay(key))
                return false;

            value = Root.Value;
            return true;
        }

        public override TValue this[TKey key]
        {
            get
            {
                if (!Splay(key))
                    throw new KeyNotFoundException(string.Format("The key {0} was not found in the collection.", key));

                return Root.Value;
            }
            set
            {
                Add(key, value);
            }
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

            // Start Dispose from the last node
            Root.SiftRight(_traversalActions);

            Root = null;
            Count = 0;
        }

        #endregion

        #region Helpers

        Node<TKey, TValue> Find(TKey key)
        {
            if (Count == 0)
                return null;

            return Root.Find(key, _traversalActions);
        }

        Node<TKey, TValue> FindNear(TKey key)
        {
            if (Count == 0)
                return null;

            // Find the place in the tree, where the key should be inserted
            // Traverse the tree and store a reference to the last encountered node
            // NOTE: We cannot find the closest node, we would need to have
            // a metric defined on keys.
            Node<TKey, TValue> lastNode = null;

            _traversalActions.SetKeyActions(keyPreAction: (n, searchKey) =>
            {
                Debug.Assert(n != null);
                lastNode = n;
                return true;
            });

            Node<TKey, TValue> exact = Root.Find(key, _traversalActions);
            Debug.Assert(lastNode != null); // Count > 0: there must be at least root
            Debug.Assert(exact == null || exact == lastNode); // If we found an exact key match; it should be equal to lastNode

            return lastNode;
        }


        private bool Splay(TKey key)
        {
            Node<TKey, TValue> node = Find(key);

            if (node == null)
                return false;

            node.Splay(out Root);
            return true;
        }

        #endregion
    }
}
