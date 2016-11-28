using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Utils.DataStructures.Nodes;

namespace Utils.DataStructures.Internal
{
    internal class NodeTraversalActions<TKey, TValue, TNode, TNodeAction>
        where TKey : struct
        where TValue : IEquatable<TValue>
        where TNode : NodeItem<TKey, TValue>
        where TNodeAction : struct
    {
        public delegate bool NodeTraversalAction(TNode node);
        public delegate bool NodeKeyTraversalAction(TNode node, TKey searchKey);


        #region Fields

        private NodeTraversalAction _preAction;
        private NodeTraversalAction _inAction;
        private NodeTraversalAction _postAction;

        private NodeKeyTraversalAction _keyPreAction;
        private NodeKeyTraversalAction _keyPostAction;

        private Stack<NodeTraversalToken<TNode, TNodeAction>> _traversalStack;

        #endregion

        #region Properties

        public bool HasPreAction { get { return _preAction != null; } }
        public bool HasInAction { get { return _inAction != null; } }
        public bool HasPostAction { get { return _postAction != null; } }

        public IComparer<TKey> KeyComparer { get; private set; }

        public Stack<NodeTraversalToken<TNode, TNodeAction>> TraversalStack
        {
            get { return _traversalStack = _traversalStack ?? new Stack<NodeTraversalToken<TNode, TNodeAction>>(); }
        }

        #endregion


        public NodeTraversalActions(IComparer<TKey> keyComparer = null)
        {
            KeyComparer = keyComparer ?? Comparer<TKey>.Default;
        }


        #region Action setters

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetActions(NodeTraversalAction preAction = null, NodeTraversalAction inAction = null, NodeTraversalAction postAction = null)
        {
            _preAction = preAction;
            _inAction = inAction;
            _postAction = postAction;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetKeyActions(NodeKeyTraversalAction keyPreAction = null, NodeKeyTraversalAction keyPostAction = null)
        {
            _keyPreAction = keyPreAction;
            _keyPostAction = keyPostAction;
        }

        #endregion

        #region Action invokation

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokePreAction(TNode node)
        {
            return !HasPreAction || _preAction(node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokeInAction(TNode node)
        {
            return !HasInAction || _inAction(node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokePostAction(TNode node)
        {
            return !HasPostAction || _postAction(node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokeKeyPreAction(TNode node, TKey searchKey)
        {
            return _keyPreAction == null || _keyPreAction(node, searchKey);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokeKeyPostAction(TNode node, TKey searchKey)
        {
            return _keyPostAction == null || _keyPostAction(node, searchKey);
        }

        #endregion
    }
}
