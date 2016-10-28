using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Utils.DataStructures
{
    public class SplayTree<TKey, TValue>
        : DictionaryBase<TKey, TValue>
        where TKey : struct
        where TValue : IEquatable<TValue>
    {
        #region Nested classes

        private class Node
            : NodeItem, IDisposable
        {
            #region Fields

            public Node Parent;

            public Node LeftChild;
            public Node RightChild;

            #endregion

            #region Genesis

            public Node(TKey key, TValue value)
                : base(key, value)
            { }

            #endregion

            #region Properties

            private bool IsLeftChild { get { return Parent != null && Parent.LeftChild == this; } }
            private bool IsRightChild { get { return Parent != null && Parent.RightChild == this; } }

            #endregion

            #region IDisposable overrides and clearing

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            #region Rotations

            public void Splay()
            {

            }

            #endregion

            #region Tree traversal

            /// <summary>
            /// Traverses the binary search tree looking for the searchKey.
            /// If no exact match is found in the tree, returns null.
            /// </summary>
            /// <returns></returns>
            public Node Sift(TKey searchKey)
            {
                int comp = Comparer<TKey>.Default.Compare(searchKey, Key);

                if (comp == 0)
                    return this;

                if (comp < 0)
                {
                    if (LeftChild == null)
                        return null;
                    return LeftChild.Sift(searchKey);
                }

                if (RightChild == null)
                    return null;
                return RightChild.Sift(searchKey);
            }

            public Node Sift(TKey searchKey, NodeTraversalActions nodeActions)
            {
                int comp = Comparer<TKey>.Default.Compare(searchKey, Key);

                if (comp == 0)
                    return this;

                nodeActions.InvokeKeyPreAction(this, searchKey);

                if (comp < 0)
                {
                    if (LeftChild == null)
                        return null;
                    return LeftChild.Sift(searchKey);
                }

                if (RightChild == null)
                    return null;
                return RightChild.Sift(searchKey);
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
            public delegate bool NodeKeyTraversalAction(Node node, TKey searchKey);


            public NodeTraversalAction PreAction;
            public NodeTraversalAction InAction;
            public NodeTraversalAction PostAction;

            public NodeKeyTraversalAction KeyPreAction;


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetActions(NodeTraversalAction preAction = null, NodeTraversalAction inAction = null, NodeTraversalAction postAction = null)
            {
                PreAction = preAction;
                InAction = inAction;
                PostAction = postAction;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetKeyActions(NodeKeyTraversalAction keyPreAction = null)
            {
                KeyPreAction = keyPreAction;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool InvokePreAction(Node node)
            {
                return PreAction == null || PreAction(node);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool InvokeInAction(Node node)
            {
                return InAction == null || InAction(node);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool InvokePostAction(Node node)
            {
                return PostAction == null || PostAction(node);
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool InvokeKeyPreAction(Node node, TKey searchKey)
            {
                return KeyPreAction == null || KeyPreAction(node, searchKey);
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
        { }

        #endregion

        #region Enumeration

        public override ItemCollection<NodeItem> Items()
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

        #endregion

        #region IDictionary<> overrides

        public override int Count { get; protected set; }
        public override bool IsReadOnly { get { return false; } }


        public override void Add(TKey key, TValue value)
        {
            // 1. Find the parent of the place in the tree, where the key should be inserted
            Node near = FindNear(key);

            // If the tree is empty, insert new root
            if (near == null)
            {
                Debug.Assert(Count == 0);
                _root = new Node(key, value);
                Count++;
                return;
            }

            // 2. Insert the key/value
            int comp = Comparer<TKey>.Default.Compare(key, near.Key);

            // If we found an exact key match, just alter the node's value
            if (comp == 0)
            {
                near.Value = value;
                return;
            }

            // The key is not present in the tree, create a new node for it
            Node newNode = new Node(key, value)
            {
                Parent = near,
            };

            if (comp < 0)
                near.LeftChild = newNode;
            else
                near.RightChild = newNode;

            Count++;

            // 3. Splay the newly inserted node to the root
            newNode.Splay();
        }

        public override bool Remove(TKey key)
        {
            if (!Splay(key))
                return false;

            // Root is now the node to be removed
            Debug.Assert(_root != null);

            Node leftTree = _root.LeftChild;

            // 1. If the root's left subtree is empty, the root will start with the right subtree
            if (leftTree == null)
            {
                Node root = _root;
                _root = root.RightChild;
                if (_root != null)
                    _root.Parent = null;
                root.Dispose();
                Count--;
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
            Debug.Assert(rightMost != null); // Count > 0: there should be at least the root

            // 3. Splay the right-most node
            // Remove the parent of root's left child to not splay up to root
            leftTree.Parent = null;
            rightMost.Splay();

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
                if (Splay(key))
                    _root.Value = value;
                else
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

            _root.SiftLeft(_traversalActions);

            _root = null;
            Count = 0;
        }

        #endregion

        #region Helpers

        Node Find(TKey key)
        {
            if (Count == 0)
                return null;

            return _root.Sift(key);
        }

        Node FindNear(TKey key)
        {
            if (Count == 0)
                return null;

            // Find the place in the tree, where the key should be inserted
            // Traverse the tree and store a reference to the last encountered node
            Node lastNode = null;

            _traversalActions.SetKeyActions(keyPreAction: (n, searchKey) =>
            {
                lastNode = n;
                return true;
            });

            Node nearest = _root.Sift(key, _traversalActions);
            Debug.Assert(lastNode != null); // Count > 0: there must be at least root
            Debug.Assert(nearest == null || nearest == lastNode); // If we found an exact key match; it should be equal to lastNode

            return lastNode;
        }

        private bool Splay(TKey key)
        {
            Node node = Find(key);

            if (node == null)
                return false;

            node.Splay();
            return true;
        }

        #endregion
    }
}
