using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Utils.DataStructures.SplayTree
{
    internal class Node<TKey, TValue>
            : DictionaryBase<TKey, TValue>.NodeItem, IDisposable
        where TKey : struct
        where TValue : IEquatable<TValue>
    {
        #region Fields

        public Node<TKey, TValue> Parent;

        public Node<TKey, TValue> LeftChild;
        public Node<TKey, TValue> RightChild;

        #endregion

        #region Properties

        // Flipping of accessors adds one check for every access but enables us
        // to avoid duplication (mirroring) of code (tree traversal and rotations)

        #region Children getters and setters

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node<TKey, TValue> GetLeftChild<TFlip>()
            where TFlip : FlipBase<TFlip>
        {
            if (FlipBase<TFlip>.FlipChildren)
                return RightChild;

            return LeftChild;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node<TKey, TValue> GetRightChild<TFlip>()
            where TFlip : FlipBase<TFlip>
        {
            if (FlipBase<TFlip>.FlipChildren)
                return LeftChild;

            return RightChild;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLeftChild<TFlip>(Node<TKey, TValue> node)
            where TFlip : FlipBase<TFlip>
        {
            if (FlipBase<TFlip>.FlipChildren)
                RightChild = node;
            else
                LeftChild = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRightChild<TFlip>(Node<TKey, TValue> node)
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

        public void Splay(ref Node<TKey, TValue> root)
        {
            while (Parent != null)
            {
                if (Parent.Parent == null)
                    Zig();
                else
                    ZigZxg();
            }

            root = this;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Zig()
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

            parent.Parent = this;
            SetRightChild<T>(parent);

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
            {
                Debug.Assert(IsRightChild<T>(), "This node is neither left nor the right child.... ?");
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
        /// is not present in the data structure.</returns>
        public Node<TKey, TValue> Find(TKey searchKey, NodeTraversalActions<TKey, TValue> nodeActions)
        {
            int comp = nodeActions.KeyComparer.Compare(searchKey, Key);

            if (comp == 0)
                return this;

            if (!nodeActions.InvokeKeyPreAction(this, searchKey))
                return null;

            try
            {
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
            finally
            {
                nodeActions.InvokeKeyPostAction(this, searchKey);
            }
        }

        /// <summary>
        /// DFS traversal of the binary search tree.
        /// If the type parameter is <see cref="NoFlip"/>, nodes are iterated from the smallest
        /// to the largest key (left to right); if the parameter is <see cref="DoFlip"/>,
        /// nodes are iterated from the largest to the smallest key (right to left).
        /// The False return value of the action functions will result in early termination of the traversal.
        /// </summary>
        /// <returns>False if an early termination of the recursion is requested.</returns>
        public bool Sift<T>(NodeTraversalActions<TKey, TValue> nodeActions)
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
}
