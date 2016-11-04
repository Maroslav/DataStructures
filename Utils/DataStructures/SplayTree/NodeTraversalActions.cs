using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Utils.DataStructures.Internal
{
    internal class NodeTraversalActions<TKey, TValue>
        where TKey : struct
        where TValue : IEquatable<TValue>
    {
        public delegate bool NodeTraversalAction(Node<TKey, TValue> node);
        public delegate bool NodeKeyTraversalAction(Node<TKey, TValue> node, TKey searchKey);


        private NodeTraversalAction _preAction;
        private NodeTraversalAction _inAction;
        private NodeTraversalAction _postAction;

        private NodeKeyTraversalAction _keyPreAction;
        private NodeKeyTraversalAction _keyPostAction;


        public IComparer<TKey> KeyComparer = Comparer<TKey>.Default;


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
        public bool InvokePreAction(Node<TKey, TValue> node)
        {
            return _preAction == null || _preAction(node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokeInAction(Node<TKey, TValue> node)
        {
            return _inAction == null || _inAction(node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokePostAction(Node<TKey, TValue> node)
        {
            return _postAction == null || _postAction(node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokeKeyPreAction(Node<TKey, TValue> node, TKey searchKey)
        {
            return _keyPreAction == null || _keyPreAction(node, searchKey);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InvokeKeyPostAction(Node<TKey, TValue> node, TKey searchKey)
        {
            return _keyPostAction == null || _keyPostAction(node, searchKey);
        }
    }
}
