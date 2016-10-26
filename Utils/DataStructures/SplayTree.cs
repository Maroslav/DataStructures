using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils.DataStructures
{
    public class SplayTree<TKey, TValue>
        : IDictionary<TKey, TValue>
        where TKey : struct
        where TValue : IEquatable<TValue>
    {
        #region Nested classes

        class Node
            : IDisposable
        {
            #region Fields

            public TKey Key;
            public TValue Value;

            public Node Parent { get; set; }

            public Node LeftChild { get; set; }
            public Node RightChild { get; set; }

            #endregion

            #region IDisposable overrides and clearing

            public void Clear()
            {
                Value = default(TValue);
                Parent = null;
                LeftChild = null;
                RightChild = null;
            }

            public void Dispose()
            {
                Clear();
            }

            #endregion

            #region Tree traversal

            public Node Sift(TKey key)
            {
                int comp = Comparer<TKey>.Default.Compare(key, Key);

                if (comp == 0)
                    return this;

                if (comp < 0)
                {
                    if (LeftChild == null)
                        return null;
                    return LeftChild.Sift(key);
                }

                if (RightChild == null)
                    return null;
                return RightChild.Sift(key);
            }

            /// <summary>
            /// Left DFS traversal of the binary search tree.
            /// The False return value of the action functions will result in early termination of the traversal.
            /// </summary>
            /// <returns>False if an early termination of the recursion is requested.</returns>
            public bool SiftLeft(NodeTraversalActions nodeActions)
            {
                if (nodeActions.InvokePreAction(this))
                    return false;

                if (LeftChild != null && !LeftChild.SiftLeft(nodeActions))
                    return false;

                if (nodeActions.InvokeInAction(this))
                    return false;

                if (RightChild != null && !RightChild.SiftLeft(nodeActions))
                    return false;

                if (nodeActions.InvokePostAction(this))
                    return false;

                return true;
            }

            /// <summary>
            /// Right DFS traversal of the binary search tree.
            /// The False return value of the action functions will result in early termination of the traversal.
            /// </summary>
            /// <returns>False if an early termination of the recursion is requested.</returns>
            public bool SiftRight(NodeTraversalActions nodeActions)
            {
                if (nodeActions.InvokePreAction(this))
                    return false;

                if (RightChild != null && !RightChild.SiftLeft(nodeActions))
                    return false;

                if (nodeActions.InvokeInAction(this))
                    return false;

                if (LeftChild != null && !LeftChild.SiftLeft(nodeActions))
                    return false;

                if (nodeActions.InvokePostAction(this))
                    return false;

                return true;
            }

            #endregion
        }

        class NodeTraversalActions
        {
            public delegate bool NodeTraversalAction(Node node);

            public NodeTraversalAction PreAction;
            public NodeTraversalAction InAction;
            public NodeTraversalAction PostAction;

            public void SetActions(NodeTraversalAction preAction = null, NodeTraversalAction inAction = null, NodeTraversalAction postAction = null)
            {
                PreAction = preAction;
                InAction = inAction;
                PostAction = postAction;
            }

            public bool InvokePreAction(Node node)
            {
                return PreAction == null || PreAction(node);
            }

            public bool InvokeInAction(Node node)
            {
                return InAction == null || InAction(node);
            }

            public bool InvokePostAction(Node node)
            {
                return PostAction == null || PostAction(node);
            }
        }

        #endregion

        #region Fields

        private Node _root;

        // Local variable to reduce stack load during recursion (we assume single-threaded usage)
        private readonly NodeTraversalActions _traversalActions = new NodeTraversalActions();

        #endregion

        #region Genesis

        public SplayTree()
        {
            _root = new Node();
        }

        #endregion

        #region IDictionary<> overrides

        public int Count { get; private set; }
        public bool IsReadOnly { get { return false; } }

        public ICollection<TKey> Keys { get; }
        public ICollection<TValue> Values { get; }


        #region Enumeration

        private IEnumerable<KeyValuePair<TKey, TValue>> GetEnumerable()
        {
            return Keys.Select(k => new KeyValuePair<TKey, TValue>(k, this[k]));
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return GetEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion


        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Add(TKey key, TValue value)
        {
        }


        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            TValue val;
            if (!TryGetValue(item.Key, out val))
                return false;

            if (!val.Equals(item.Value))
                return false;

            Remove(item.Key);
            return true;
        }

        public bool Remove(TKey key)
        {
            if (!Splay(key))
                return false;

            // Root is now the node to be removed
            Debug.Assert(_root != null);

            Node leftTree = _root.LeftChild;

            // 1. If the left subtree is empty, the root will start with the right subtree
            if (leftTree == null)
            {
                Node root = _root;
                _root = root.RightChild;
                if (_root != null)
                    _root.Parent = null;
                root.Dispose();
                return true;
            }

            // 2. Find the right-most node in the root's left subtree -- it will become the new root
            Node rightMost = null;

            _traversalActions.SetActions(inAction: n =>
            {
                rightMost = n;
                return false; // Terminate the DFS when we find the first node
            });

            leftTree.SiftRight(_traversalActions);
            Debug.Assert(rightMost != null);

            // 3. Splay the right-most node
            // Remove the parent of root's left child to not splay up to root
            leftTree.Parent = null;
            Splay(rightMost);

            // 4. Right-most is now root of the left tree (and has no right subtree); merge it with Root
            leftTree = rightMost;
            Debug.Assert(leftTree.RightChild == null);

            leftTree.RightChild = _root.RightChild;
            if (leftTree.RightChild != null)
                leftTree.RightChild.Parent = leftTree;

            _root.Clear();
            _root = leftTree;

            return true;
        }


        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);

            if (!Splay(key))
                return false;

            value = _root.Value;
            return true;
        }

        public TValue this[TKey key]
        {
            get
            {
                if (!Splay(key))
                    throw new KeyNotFoundException(string.Format("The key {0} was not found in the collection.", key));

                return _root.Value;
            }
            set
            {
                if (Splay(key))
                    _root.Value = value;
                else
                    Add(key, value);
            }
        }


        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue val;

            return TryGetValue(item.Key, out val) && val.Equals(item.Value);
        }

        public bool ContainsKey(TKey key)
        {
            return Splay(key);
        }


        public void Clear()
        {
            _traversalActions.SetActions(postAction: n =>
            {
                n.Dispose();
                return true;
            });

            _root.SiftLeft(_traversalActions);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex", "The arrayIndex must not be negative.");
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Not enough space in the array.", "array");

            int i = arrayIndex;

            foreach (var keyValuePair in GetEnumerable())
                array[i++] = keyValuePair;
        }

        #endregion

        #region Helpers

        Node Find(TKey key)
        {
            return _root.Sift(key);
        }

        private bool Splay(TKey key)
        {
            Node node = Find(key);

            if (node == null)
                return false;

            Splay(node);
            return true;
        }

        private void Splay(Node node)
        {

        }

        #endregion
    }
}
