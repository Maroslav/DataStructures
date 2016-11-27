using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Utils.DataStructures.Nodes;

namespace Utils.DataStructures
{
    public abstract class HeapBase<TKey, TValue>
        : DictionaryBase<TKey, TValue>, IPriorityQueue<TKey, TValue>
    {
        protected IComparer<TKey> Comparer { get; private set; }


        public HeapBase(IComparer<TKey> keyComparer = null)
        {
            Comparer = keyComparer ?? Comparer<TKey>.Default;
        }


        #region DictionaryBase<,> overrides

        public override int Count { get; protected set; }

        public sealed override bool TryGetValue(TKey key, out TValue value)
        {
            throw new System.NotImplementedException();
        }

        public sealed override bool Remove(TKey key)
        {
            throw new System.NotImplementedException();
        }

        public sealed override TValue this[TKey key]
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        #endregion

        #region IPriorityQueue<,> overrides

        public abstract NodeItem<TKey, TValue> PeekMin();

        public abstract void DecreaseKey(NodeItem<TKey, TValue> node, TKey newKey);
        public abstract void DeleteMin();
        public abstract void Delete(NodeItem<TKey, TValue> node);

        public abstract void Merge(IPriorityQueue<TKey, TValue> other);

        #endregion

        #region Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Swap<T>(ref T one, ref T two)
        {
            T tmp = one;
            one = two;
            two = tmp;
        }

        #endregion
    }
}
