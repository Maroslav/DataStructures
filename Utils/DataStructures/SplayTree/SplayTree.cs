using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Utils.DataStructures.SplayTree
{
    public class SplayTree<TKey, TValue>
        : DictionaryBase<TKey, TValue>
        where TKey : struct
        where TValue : IEquatable<TValue>
    {
        #region Fields

        private Node<TKey, TValue> _root;

        // Local variable to reduce stack load during recursion (we assume single-threaded usage)
        private readonly NodeTraversalActions<TKey, TValue> _traversalActions;

        #endregion

        #region Genesis

        public SplayTree(IComparer<TKey> keyComparer = null)
        {
            _traversalActions = new NodeTraversalActions<TKey, TValue>();

            if (keyComparer != null)
                _traversalActions.KeyComparer = keyComparer;
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

                _traversalActions.SetActions(preAction: n =>
                {
                    items[i++] = n;
                    return true;
                });

                _root.SiftLeft(_traversalActions);

                return new ItemCollection<NodeItem>(items, Count);
            }
        }

        public override string ToString()
        {
            return _root != null ? _root.ToString() : string.Empty;
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
                _root = new Node<TKey, TValue>(key, value);
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
            newNode.Splay(out _root);
        }

        public override bool Remove(TKey key)
        {
            if (!Splay(key))
                return false;

            // Root is now the node to be removed
            Debug.Assert(_root != null);

            Node<TKey, TValue> leftTree = _root.LeftChild;

            // 1. If the root's left subtree is empty, the root will start with the right subtree
            if (leftTree == null)
            {
                Node<TKey, TValue> oldRoot = _root;
                _root = oldRoot.RightChild;
                if (_root != null)
                    _root.Parent = null;
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
            rightMost.Splay(out _root);

            // 4. Right-most is now root of the left tree (and has no right subtree); merge it with Root
            leftTree = rightMost;
            Debug.Assert(leftTree.RightChild == null); // Splay on the right-most node should make it have no right (larger) children

            leftTree.RightChild = _root.RightChild;
            if (leftTree.RightChild != null)
                leftTree.RightChild.Parent = leftTree;

            _root.Clear();
            _root = leftTree;
            Count--;

            return true;
        }


        public override bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);

            if (!Splay(key))
                return false;

            value = _root.Value;
            return true;
        }

        public override TValue this[TKey key]
        {
            get
            {
                if (!Splay(key))
                    throw new KeyNotFoundException(string.Format("The key {0} was not found in the collection.", key));

                return _root.Value;
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
            _root.SiftRight(_traversalActions);

            _root = null;
            Count = 0;
        }

        #endregion

        #region Helpers

        Node<TKey, TValue> Find(TKey key)
        {
            if (Count == 0)
                return null;

            return _root.Find(key, _traversalActions);
        }

        Node<TKey, TValue> FindNear(TKey key)
        {
            if (Count == 0)
                return null;

            // Find the place in the tree, where the key should be inserted
            // Traverse the tree and store a reference to the last encountered node
            Node<TKey, TValue> lastNode = null;

            _traversalActions.SetKeyActions(keyPreAction: (n, searchKey) =>
            {
                lastNode = n;
                return true;
            });

            Node<TKey, TValue> exact = _root.Find(key, _traversalActions);
            Debug.Assert(lastNode != null); // Count > 0: there must be at least root
            Debug.Assert(exact == null || exact == lastNode); // If we found an exact key match; it should be equal to lastNode

            return lastNode;
        }


        private bool Splay(TKey key)
        {
            Node<TKey, TValue> node = Find(key);

            if (node == null)
                return false;

            node.Splay(out _root);
            return true;
        }

        #endregion
    }
}
