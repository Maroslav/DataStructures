using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Utils.DataStructures
{
    public abstract class DictionaryBase<TKey, TValue>
        : IDictionary<TKey, TValue>, IEnumerable<DictionaryBase<TKey, TValue>.NodeItem>
    {
        #region Nested classes

        public class NodeItem
        {
            // Key is immutable
            public TKey Key { get; protected set; }
            // We allow the value to be mutable (only valid if it is a reference type
            public TValue Value { get; set; }


            public NodeItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        public class ItemCollection<T>
            : ICollection<T>
        {
            #region Fields

            private readonly IEnumerable<T> _values;
            private readonly int _count;

            #endregion

            #region Genesis

            internal ItemCollection(IEnumerable<T> values, int count)
            {
                if (values == null)
                    throw new ArgumentNullException("values");

                _values = values;
                _count = count;
                Debug.Assert(_count == _values.Count());
            }

            #endregion

            #region ICollection<> overrides

            public int Count { get { return _count; } }
            public bool IsReadOnly { get { return true; } }


            public IEnumerator<T> GetEnumerator()
            {
                return _values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }


            #region Hidded (explicit) disabled overrides

            void ICollection<T>.Add(T item)
            {
                throw Throw();
            }

            bool ICollection<T>.Remove(T item)
            {
                throw Throw();
            }

            void ICollection<T>.Clear()
            {
                throw Throw();
            }

            #endregion


            public bool Contains(T item)
            {
                return _values.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                DictionaryBase<TKey, TValue>.CopyTo(_values, Count, array, arrayIndex);
            }

            #endregion

            #region Helpers

            private static NotSupportedException Throw()
            {
                throw new NotSupportedException("Modifying the ItemCollection is not allowed.");
            }

            #endregion
        }

        #endregion

        #region Properties

        // Note: explicit implementation of interface members will make them private unless viewing the object as the interface itself
        // When working with DictionaryBase or derived types, users will see these implementations of Keys and Values (rather than
        // the explicit implementations). ItemCollection<> itself hides some members unless viewed as an ICollection<>
        public virtual ItemCollection<TKey> Keys { get { return new ItemCollection<TKey>(Items.Select(node => node.Key), Count); } }
        public virtual ItemCollection<TValue> Values { get { return new ItemCollection<TValue>(Items.Select(node => node.Value), Count); } }

        // NodeItem allows modification of values
        public abstract ItemCollection<NodeItem> Items { get; }

        #endregion

        #region IDictionary<> overrides

        public abstract int Count { get; protected set; }
        public abstract bool IsReadOnly { get; }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys { get { return Keys; } }
        ICollection<TValue> IDictionary<TKey, TValue>.Values { get { return Values; } }


        #region Enumeration

        public virtual IEnumerator<NodeItem> GetEnumerator()
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


        public abstract void Add(TKey key, TValue value);

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
            CopyTo(this, Count, array, arrayIndex);
        }

        #endregion

        #region Helpers

        private static void CopyTo<T>(IEnumerable<T> source, int sourceCount, T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex", "The arrayIndex must not be negative.");
            if (array.Length - arrayIndex < sourceCount)
                throw new ArgumentException("Not enough space in the array.", "array");

            int i = arrayIndex;

            foreach (var keyValuePair in source)
                array[i++] = keyValuePair;
        }

        #endregion
    }
}
