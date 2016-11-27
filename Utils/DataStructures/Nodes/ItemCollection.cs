using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils.DataStructures.Nodes
{
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
            _values.CopyTo(Count, array, arrayIndex);
        }

        #endregion

        #region Helpers

        private static NotSupportedException Throw()
        {
            throw new NotSupportedException("Modifying the ItemCollection is not allowed.");
        }

        #endregion

        #region ToString()

        public override string ToString()
        {
            return ToString(item => item.ToString());
        }

        public string ToString(Func<T, string> selector)
        {
            var sb = new StringBuilder("{ ");

            foreach (var value in _values)
            {
                sb.Append(selector(value));
                sb.Append(", ");
            }

            sb.Append(" }");

            return sb.ToString();
        }

        #endregion
    }
}
