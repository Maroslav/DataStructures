using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Utils.DataStructures.Internal
{
    internal class Node<TKey, TValue>
            : DictionaryBase<TKey, TValue>.NodeItem, IDisposable
        where TKey : struct
        where TValue : IEquatable<TValue>
    {
        #region Nested classes

        #region Flipping

        internal class FlipBase<TDoFlipTrait>
            where TDoFlipTrait : FlipBase<TDoFlipTrait>
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

        internal sealed class NoFlip
            : FlipBase<NoFlip>
        { }

        internal sealed class DoFlip
            : FlipBase<DoFlip>
        { }

        #endregion

        #region Traversal

        internal enum NodeAction
        {
            Sift,
            InAction,
            PostAction,
        }

        internal struct NodeTraversalToken
        {
            public readonly Node<TKey, TValue> Node;
            public readonly NodeAction Action;

            public NodeTraversalToken(Node<TKey, TValue> node, NodeAction action)
            {
                Node = node;
                Action = action;
            }

            public override string ToString()
            {
                return Action.ToString();
            }
        }

        #endregion

        #endregion

        #region Fields

        public Node<TKey, TValue> Parent;

        private Node<TKey, TValue> _leftChild;
        private Node<TKey, TValue> _rightChild;

        #endregion

        #region Properties

        // Flipping of accessors adds one check for every access but enables us
        // to avoid duplication (mirroring) of code (tree traversal and rotations)

        #region Children getters and setters

        public Node<TKey, TValue> LeftChild
        {
            get { return _leftChild; }
            set
            {
                _leftChild = value;
                if (_leftChild != null)
                    _leftChild.Parent = this;
            }
        }

        public Node<TKey, TValue> RightChild
        {
            get { return _rightChild; }
            set
            {
                _rightChild = value;
                if (_rightChild != null)
                    _rightChild.Parent = this;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Node<TKey, TValue> GetLeftChild<TFlip>()
            where TFlip : FlipBase<TFlip>
        {
            if (FlipBase<TFlip>.FlipChildren)
                return RightChild;

            return LeftChild;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Node<TKey, TValue> GetRightChild<TFlip>()
            where TFlip : FlipBase<TFlip>
        {
            if (FlipBase<TFlip>.FlipChildren)
                return LeftChild;

            return RightChild;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetLeftChild<TFlip>(Node<TKey, TValue> node)
            where TFlip : FlipBase<TFlip>
        {
            if (FlipBase<TFlip>.FlipChildren)
                RightChild = node;
            else
                LeftChild = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetRightChild<TFlip>(Node<TKey, TValue> node)
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
        public bool IsLeftChild()
        {
            return IsLeftChild<NoFlip>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsLeftChild<TFlip>()
            where TFlip : FlipBase<TFlip>
        {
            Debug.Assert(Parent != null);
            return Parent.GetLeftChild<TFlip>() == this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRightChild()
        {
            return IsRightChild<NoFlip>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsRightChild<TFlip>()
            where TFlip : FlipBase<TFlip>
        {
            Debug.Assert(Parent != null);
            return Parent.GetRightChild<TFlip>() == this;
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
            Dispose(ref _key);
            Dispose(ref _value);

            Parent = null;
            _leftChild = null;
            _rightChild = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Dispose<T>(ref T item)
        {
            var disp = item as IDisposable;

            if (disp != null)
                disp.Dispose();

            item = default(T);
        }

        public void Dispose()
        {
            Clear();
        }

        #endregion

        #region Rotations

        /// <summary>
        /// Splays the node all the way to the root and outputs it.
        /// This forces the caller not to forget to assign the new root somewhere..
        /// </summary>
        public void Splay(out Node<TKey, TValue> newRoot)
        {
            while (Parent != null)
            {
                if (Parent.Parent == null)
                    Zig();
                else
                    ZigZxg();
            }

            newRoot = this;
        }


        internal void Zig()
        {
            if (IsLeftChild())
                Zig<NoFlip>();
            else
            {
                Debug.Assert(IsRightChild(), "This node is neither left nor the right child.... ?");
                Zig<DoFlip>();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Zig<T>()
            where T : FlipBase<T>
        {
            if (Parent == null)
                return;

            Node<TKey, TValue> parent = Parent;
            Node<TKey, TValue> grandParent = parent.Parent;
            Node<TKey, TValue> rightTree = GetRightChild<T>();

            if (grandParent != null)
            {
                if (parent.IsLeftChild())
                    grandParent.LeftChild = this;
                else
                    grandParent.RightChild = this;
            }

            Parent = grandParent;

            parent.SetLeftChild<T>(rightTree);
            SetRightChild<T>(parent);
        }


        internal void ZigZxg()
        {
            Debug.Assert(Parent.Parent != null);

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
            {
                Debug.Assert(IsRightChild<T>(), "This node is neither the left nor the right child.... ?");
                ZigZag();
            }
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

        #region Traversal

        /// <summary>
        /// Traverses the binary search tree looking for the searchKey.
        /// If no exact match is found in the tree, returns null.
        /// </summary>
        /// <returns>The first node that matches the <see cref="searchKey"/> or null if the key
        /// is not present in the tree.</returns>
        public Node<TKey, TValue> Find(TKey searchKey, NodeTraversalActions<TKey, TValue> nodeActions)
        {
            // We have to use an iterative way because the default stack size of .net apps is 1MB
            // and it's impractical to change it.....
            var stack = nodeActions.TraversalStack;
            Debug.Assert(stack.Count == 0);
            stack.Push(new NodeTraversalToken(this, NodeAction.Sift));

            try
            {
                while (stack.Count > 0)
                {
                    var token = stack.Pop();

                    if (!nodeActions.InvokeKeyPreAction(token.Node, searchKey))
                        return null;


                    int comp = nodeActions.KeyComparer.Compare(searchKey, token.Node.Key);

                    if (comp == 0)
                        return token.Node;

                    var nextNode = comp < 0
                        ? token.Node.LeftChild
                        : token.Node.RightChild;

                    if (nextNode == null)
                        return null;
                    stack.Push(new NodeTraversalToken(nextNode, NodeAction.Sift));
                }
            }
            finally
            {
                stack.Clear();
            }

            Debug.Fail("Wth?");
            return null;
        }

        /// <summary>
        /// Traverses the binary search tree looking for the searchKey.
        /// If no exact match is found in the tree, returns null.
        /// </summary>
        /// <returns>The first node that matches the <see cref="searchKey"/> or null if the key
        /// is not present in the tree.</returns>
        internal Node<TKey, TValue> FindRecursive(TKey searchKey, NodeTraversalActions<TKey, TValue> nodeActions)
        {
            if (!nodeActions.InvokeKeyPreAction(this, searchKey))
                return null;

            int comp = nodeActions.KeyComparer.Compare(searchKey, Key);

            if (comp == 0)
                return this;

            try
            {
                if (comp < 0)
                {
                    if (LeftChild == null)
                        return null;
                    return LeftChild.FindRecursive(searchKey, nodeActions);
                }

                if (RightChild == null)
                    return null;
                return RightChild.FindRecursive(searchKey, nodeActions);
            }
            finally
            {
                nodeActions.InvokeKeyPostAction(this, searchKey);
            }
        }

        /// <summary>
        /// Left DFS traversal of the binary search tree. Nodes are traversed from the smallest to the largest key.
        /// The False return value of the action functions will result in early termination of the traversal.
        /// </summary>
        /// <returns>False if an early termination of the recursion is requested.</returns>
        public bool SiftLeft(NodeTraversalActions<TKey, TValue> nodeActions)
        {
            return Sift<NoFlip>(nodeActions);
        }

        /// <summary>
        /// Right DFS traversal of the binary search tree. Nodes are traversed from the largest to the smallest key.
        /// The False return value of the action functions will result in early termination of the traversal.
        /// </summary>
        /// <returns>False if an early termination of the recursion is requested.</returns>
        public bool SiftRight(NodeTraversalActions<TKey, TValue> nodeActions)
        {
            return Sift<DoFlip>(nodeActions);
        }

        /// <summary>
        /// If the type parameter is <see cref="NoFlip"/>, nodes are iterated from the smallest
        /// to the largest key (left to right); if the parameter is <see cref="DoFlip"/>,
        /// nodes are iterated from the largest to the smallest key (right to left).
        /// </summary>
        private bool Sift<T>(NodeTraversalActions<TKey, TValue> nodeActions)
            where T : FlipBase<T>
        {
            // We have to use an iterative way because the default stack size of .net apps is 1MB
            // and it's impractical to change it.....
            var stack = nodeActions.TraversalStack;
            Debug.Assert(stack.Count == 0);
            stack.Push(new NodeTraversalToken(this, NodeAction.Sift));

            try
            {
                while (stack.Count > 0)
                {
                    var token = stack.Pop();

                    switch (token.Action)
                    {
                        case NodeAction.Sift:
                            if (!token.Node.HandleSift<T>(stack, nodeActions))
                                return false;
                            break;

                        case NodeAction.InAction:
                            if (!nodeActions.InvokeInAction(token.Node))
                                return false;
                            break;

                        case NodeAction.PostAction:
                            if (!nodeActions.InvokePostAction(token.Node))
                                return false;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            finally
            {
                stack.Clear();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandleSift<T>(Stack<NodeTraversalToken> stack, NodeTraversalActions<TKey, TValue> nodeActions)
            where T : FlipBase<T>
        {
            // First and only visit to this node
            if (!nodeActions.InvokePreAction(this))
                return false;


            // Push actions in reverse order
            var right = GetRightChild<T>();
            var left = GetLeftChild<T>();

            if (right != null)
            {
                stack.Push(new NodeTraversalToken(this, NodeAction.PostAction));
                stack.Push(new NodeTraversalToken(right, NodeAction.Sift));
            }
            else if (left != null)
                // We need to store the action (it has to be executed after sifting through Left)
                stack.Push(new NodeTraversalToken(this, NodeAction.PostAction));

            if (left != null)
            {
                stack.Push(new NodeTraversalToken(this, NodeAction.InAction));
                stack.Push(new NodeTraversalToken(left, NodeAction.Sift));
            }
            else
            {
                // Handle missing children -- we can only invoke actions right away if children are null from left to right
                if (!nodeActions.InvokeInAction(this))
                    return false;

                if (right == null)
                    if (!nodeActions.InvokePostAction(this))
                        return false;
            }

            return true;
        }

        /// <summary>
        /// If the type parameter is <see cref="NoFlip"/>, nodes are iterated from the smallest
        /// to the largest key (left to right); if the parameter is <see cref="DoFlip"/>,
        /// nodes are iterated from the largest to the smallest key (right to left).
        /// </summary>
        internal bool SiftRecursive<T>(NodeTraversalActions<TKey, TValue> nodeActions)
            where T : FlipBase<T>
        {
            if (!nodeActions.InvokePreAction(this))
                return false;

            if (GetLeftChild<T>() != null && !GetLeftChild<T>().SiftRecursive<T>(nodeActions))
                return false;

            if (!nodeActions.InvokeInAction(this))
                return false;

            if (GetRightChild<T>() != null && !GetRightChild<T>().SiftRecursive<T>(nodeActions))
                return false;

            if (!nodeActions.InvokePostAction(this))
                return false;

            return true;
        }

        private const string ExtendPrefix = "│   ";
        private const string EmptyPrefix = "    ";
        private const string RootFork = "─── ";
        private const string RightFork = "┌── ";
        private const string LeftFork = "└── ";

        public override string ToString()
        {
            StringBuilder prefix = new StringBuilder();
            StringBuilder sb = new StringBuilder();

            NodeTraversalActions<TKey, TValue>.NodeTraversalAction preAction = node =>
            {
                Debug.Assert(node == this || node.Parent != null);

                // Compute new prefix for the right child
                prefix.Append(node.Parent != null && node.IsLeftChild() ? ExtendPrefix : EmptyPrefix);
                return true;
            };

            NodeTraversalActions<TKey, TValue>.NodeTraversalAction inAction = node =>
            {
                // Get the old prefix (revert the preAction)
                prefix.Length -= ExtendPrefix.Length;

                bool isLeftChild = node.Parent == null || node.IsLeftChild();

                // Output a new line
                sb.Append(prefix);
                if (node.Parent == null)
                    sb.Append(RootFork);
                else
                    sb.Append(isLeftChild ? LeftFork : RightFork);
                sb.AppendLine(node.Key.ToString());

                // Compute new prefix for the left child
                prefix.Append(isLeftChild ? EmptyPrefix : ExtendPrefix);
                return true;
            };

            NodeTraversalActions<TKey, TValue>.NodeTraversalAction postAction = node =>
            {
                // Get the old prefix (revert the inAction)
                prefix.Length -= ExtendPrefix.Length;
                return true;
            };

            var nodeActions = new NodeTraversalActions<TKey, TValue>(); // We do not need to pass the owner's comparer -- Sift does not use it (only Find does)
            nodeActions.SetActions(preAction, inAction, postAction);
            SiftRight(nodeActions);

            return sb.ToString();
        }

        #endregion
    }
}
