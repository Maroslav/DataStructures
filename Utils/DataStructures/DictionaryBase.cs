using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils.DataStructures
{
    public abstract class DictionaryBase<TKey, TValue>
        : IDictionary<TKey, TValue>
    {
        #region IDictionary<> overrides

        public abstract int Count { get; protected set; }
        public abstract bool IsReadOnly { get; }

        public abstract ICollection<TKey> Keys { get; }
        public abstract ICollection<TValue> Values { get; }


        #region Enumeration

        protected virtual IEnumerable<KeyValuePair<TKey, TValue>> GetEnumerable()
        {
            return Keys.Select(k => new KeyValuePair<TKey, TValue>(k, this[k]));
        }

        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return GetEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion


        public abstract void Add(TKey key, TValue value);

        public virtual void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }


        public abstract bool Remove(TKey key);

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


        public abstract bool TryGetValue(TKey key, out TValue value);
        public abstract TValue this[TKey key] { get; set; }


        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue val;
            return TryGetValue(item.Key, out val) && val.Equals(item.Value);
        }

        public bool ContainsKey(TKey key)
        {
            TValue val;
            return TryGetValue(key, out val);
        }


        public abstract void Clear();

        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
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

    }
}
