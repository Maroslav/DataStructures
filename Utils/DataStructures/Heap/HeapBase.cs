using System.Collections.Generic;
using System.Diagnostics;

namespace Utils.DataStructures
{
    public abstract class HeapBase<TKey, TValue>
        : DictionaryBase<TKey, TValue>
    {
        protected IComparer<TKey> Comparer { get; private set; }



        public HeapBase(IComparer<TKey> comparer = null)
        {
            Comparer = comparer ?? Comparer<TKey>.Default;
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

        #region Extending accessors

        public abstract NodeItem PeekMin();

        public abstract NodeItem DeleteMin();

        #endregion
    }
}
