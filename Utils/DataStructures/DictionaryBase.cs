using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Utils.DataStructures.Nodes;

namespace Utils.DataStructures
{
    public abstract class DictionaryBase<TKey, TValue>
        : IDictionary<TKey, TValue>, IEnumerable<NodeItem<TKey, TValue>>
    {
        #region Nested classes

        #endregion

        #region Properties

        // Note: explicit implementation of interface members will make them private unless viewing the object as the interface itself
        // When working with DictionaryBase or derived types, users will see these implementations of Keys and Values (rather than
        // the explicit implementations). ItemCollection<> itself hides some members unless viewed as an ICollection<>
        public virtual ItemCollection<TKey> Keys { get { return new ItemCollection<TKey>(Items.Select(node => node.Key), Count); } }
        public virtual ItemCollection<TValue> Values { get { return new ItemCollection<TValue>(Items.Select(node => node.Value), Count); } }

        // NodeItem allows modification of values
        public abstract ItemCollection<NodeItem<TKey, TValue>> Items { get; }

        #endregion

        #region IDictionary<> overrides

        public abstract int Count { get; protected set; }
        public abstract bool IsReadOnly { get; }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys { get { return Keys; } }
        ICollection<TValue> IDictionary<TKey, TValue>.Values { get { return Values; } }


        #region Enumeration

        public virtual IEnumerator<NodeItem<TKey, TValue>> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return Items.Select(node => new KeyValuePair<TKey, TValue>(node.Key, node.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion


        public abstract NodeItem<TKey,TValue> Add(TKey key, TValue value);

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            Add(key, value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }


        public abstract bool Remove(TKey key);

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
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


        public bool ContainsKey(TKey key)
        {
            TValue val;
            return TryGetValue(key, out val);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue val;
            return TryGetValue(item.Key, out val) && val.Equals(item.Value);
        }


        public abstract void Clear();

        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            this.CopyTo(Count, array, arrayIndex);
        }

        #endregion
    }
}
