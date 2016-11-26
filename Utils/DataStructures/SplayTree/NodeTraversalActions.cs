using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Utils.DataStructures.Nodes;

namespace Utils.DataStructures.Internal
{
    internal class NodeTraversalActions<TKey, TValue>
        where TKey : struct
        where TValue : IEquatable<TValue>
    {
        public delegate bool NodeTraversalAction(BinaryNode<TKey, TValue> node);
        public delegate bool NodeKeyTraversalAction(BinaryNode<TKey, TValue> node, TKey searchKey);


        private NodeTraversalAction _preAction;
        private NodeTraversalAction _inAction;
        private NodeTraversalAction _postAction;

        private NodeKeyTraversalAction _keyPreAction;
        private NodeKeyTraversalAction _keyPostAction;

        private Stack<NodeTraversalToken<BinaryNode<TKey, TValue>, BinaryNodeAction>> _traversalStack;


        public IComparer<TKey> KeyComparer { get; private set; }

        public Stack<NodeTraversalToken<BinaryNode<TKey, TValue>, BinaryNodeAction>> TraversalStack
        {
            get { return _traversalStack = _traversalStack ?? new Stack<NodeTraversalToken<BinaryNode<TKey, TValue>, BinaryNodeAction>>(); }
        }


        public NodeTraversalActions(IComparer<TKey> keyComparer = null)
        {
            KeyComparer = keyComparer ?? Comparer<TKey>.Default;
        }


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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokePreAction(BinaryNode<TKey, TValue> node)
        {
            return _preAction == null || _preAction(node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokeInAction(BinaryNode<TKey, TValue> node)
        {
            return _inAction == null || _inAction(node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokePostAction(BinaryNode<TKey, TValue> node)
        {
            return _postAction == null || _postAction(node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokeKeyPreAction(BinaryNode<TKey, TValue> node, TKey searchKey)
        {
            return _keyPreAction == null || _keyPreAction(node, searchKey);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokeKeyPostAction(BinaryNode<TKey, TValue> node, TKey searchKey)
        {
            return _keyPostAction == null || _keyPostAction(node, searchKey);
        }
    }
}
