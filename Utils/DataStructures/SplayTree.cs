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

        #region Node family access flipping classes

        private class FlipBase<TDoFlipTrait>
        {
            public static bool FlipChildren;

            static FlipBase()
            {
                if (typeof(TDoFlipTrait) == typeof(NoFlip))
                    FlipChildren = false;
                else if (typeof(TDoFlipTrait) == typeof(DoFlip))
                    FlipChildren = true;
                else
                    throw new TypeLoadException(string.Format("Invalid type parameter {0} for the FlipBase class.", typeof(TDoFlipTrait).Name));
            }
        }

        sealed class NoFlip
            : FlipBase<NoFlip>
        { }

        sealed class DoFlip
            : FlipBase<DoFlip>
        { }

        #endregion

        private class Node
            : NodeItem, IDisposable
        {
            #region Fields

            public Node Parent;

            public Node LeftChild;
            public Node RightChild;

            #endregion

            #region Properties

            // Flipping of accessors adds one check for every access but enables us
            // to avoid duplication (mirroring) of code (tree traversal and rotations)

            #region Children getters and setters

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node GetLeftChild<TFlip>()
                where TFlip : FlipBase<TFlip>
            {
                if (FlipBase<TFlip>.FlipChildren)
                    return RightChild;

                return LeftChild;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node GetRightChild<TFlip>()
                where TFlip : FlipBase<TFlip>
            {
                if (FlipBase<TFlip>.FlipChildren)
                    return LeftChild;

                return RightChild;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetLeftChild<TFlip>(Node node)
                where TFlip : FlipBase<TFlip>
            {
                if (FlipBase<TFlip>.FlipChildren)
                    RightChild = node;
                else
                    LeftChild = node;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetRightChild<TFlip>(Node node)
                where TFlip : FlipBase<TFlip>
            {
                if (FlipBase<TFlip>.FlipChildren)
                    LeftChild = node;
                else
                    RightChild = node;
            }

            #endregion

            #region Family getters

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node GetGrandParent()
            {
                if (Parent == null)
                    return null;

                return Parent.Parent;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsLeftChild()
            {
                return IsLeftChild<NoFlip>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsLeftChild<TFlip>()
                where TFlip : FlipBase<TFlip>
            {
                return Parent != null && Parent.GetLeftChild<TFlip>() == this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsRightChild()
            {
                return IsRightChild<NoFlip>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsRightChild<TFlip>()
                where TFlip : FlipBase<TFlip>
            {
                return Parent != null && Parent.GetRightChild<TFlip>() == this;
            }

            #endregion

            #endregion

            #region Genesis

            public Node(TKey key, TValue value)
                : base(key, value)
            { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Clear()
            {
                var disp = Key as IDisposable;
                if (disp != null)
                    disp.Dispose();
                Key = default(TKey);

                disp = Value as IDisposable;
                if (disp != null)
                    disp.Dispose();
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
                while (Parent != null)
                {
                    if (GetGrandParent() == null)
                        Zig();
                    else
                        ZigZxg();
                }
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected void Zig()
            {
                if (IsLeftChild())
                    Zig<NoFlip>();
                else if (IsRightChild())
                    Zig<DoFlip>();
                else
                    Debug.Fail("This node is neither left nor the right child.... ?");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Zig<T>()
                where T : FlipBase<T>
            {
                if (Parent == null)
                    return;

                Node parent = Parent;
                Node grandParent = parent.Parent;
                Node rightTree = GetRightChild<T>();

                SetRightChild<T>(parent);
                GetRightChild<T>().Parent = this;
                Debug.Assert(GetRightChild<T>().GetLeftChild<T>() == this);
                GetRightChild<T>().SetLeftChild<T>(rightTree);

                Parent = grandParent;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected void ZigZxg()
            {
                if (Parent.IsLeftChild())
                    ZigZxg<NoFlip>();
                else
                    ZigZxg<DoFlip>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ZigZxg<T>()
                where T : FlipBase<T>
            {
                Debug.Assert(Parent.IsLeftChild<T>());

                if (IsLeftChild<T>())
                    ZigZig();
                else
                    ZigZag();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ZigZag()
            {
                Zig();
                Zig();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ZigZig()
            {
                Parent.Zig();
                Zig();
            }

            #endregion

            #region Tree traversal

            /// <summary>
            /// Traverses the binary search tree looking for the searchKey.
            /// If no exact match is found in the tree, returns null.
            /// </summary>
            /// <returns>The first node that matches the <see cref="searchKey"/> or null if the key
            /// is not present in the data structure.</returns>
            public Node Find(TKey searchKey)
            {
                int comp = Comparer<TKey>.Default.Compare(searchKey, Key);

                if (comp == 0)
                    return this;

                if (comp < 0)
                {
                    if (LeftChild == null)
                        return null;
                    return LeftChild.Find(searchKey);
                }

                if (RightChild == null)
                    return null;
                return RightChild.Find(searchKey);
            }

            // The other overload with nearly the same body is there just to reduce the recursion cost.
            public Node Find(TKey searchKey, NodeTraversalActions nodeActions)
            {
                int comp = Comparer<TKey>.Default.Compare(searchKey, Key);

                if (comp == 0)
                    return this;

                if (!nodeActions.InvokeKeyPreAction(this, searchKey))
                    return null;

                if (comp < 0)
                {
                    if (LeftChild == null)
                        return null;
                    return LeftChild.Find(searchKey, nodeActions);
                }

                if (RightChild == null)
                    return null;
                return RightChild.Find(searchKey, nodeActions);
            }

            /// <summary>
            /// DFS traversal of the binary search tree.
            /// If the type parameter is <see cref="NoFlip"/>, nodes are iterated from the smallest
            /// to the largest key (left to right); if the parameter is <see cref="DoFlip"/>,
            /// nodes are iterated from the largest to the smallest key (right to left).
            /// The False return value of the action functions will result in early termination of the traversal.
            /// </summary>
            /// <returns>False if an early termination of the recursion is requested.</returns>
            public bool Sift<T>(NodeTraversalActions nodeActions)
                where T : FlipBase<T>
            {
                if (nodeActions.InvokePreAction(this))
                    return false;

                if (GetLeftChild<T>() != null && !GetLeftChild<T>().Sift<T>(nodeActions))
                    return false;

                if (nodeActions.InvokeInAction(this))
                    return false;

                if (GetRightChild<T>() != null && !GetRightChild<T>().Sift<T>(nodeActions))
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

            _root.Sift<NoFlip>(_traversalActions);

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
                Node oldRoot = _root;
                _root = oldRoot.RightChild;
                if (_root != null)
                    _root.Parent = null;
                oldRoot.Dispose();
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

            leftTree.Sift<DoFlip>(_traversalActions);
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
            _root.Sift<DoFlip>(_traversalActions);

            _root = null;
            Count = 0;
        }

        #endregion

        #region Helpers

        Node Find(TKey key)
        {
            if (Count == 0)
                return null;

            return _root.Find(key);
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

            Node near = _root.Find(key, _traversalActions);
            Debug.Assert(lastNode != null); // Count > 0: there must be at least root
            Debug.Assert(near == null || near == lastNode); // If we found an exact key match; it should be equal to lastNode

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
