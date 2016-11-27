using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Utils.DataStructures.Nodes
{
    public class NodeItem<TKey, TValue>
        : IDisposable
    {
        protected TKey _key;
        protected TValue _value;

        // Key is immutable
        public TKey Key
        {
            get { return _key; }
            internal set { _key = value; }
        }

        // We allow the value to be mutable (only valid if it is a reference type)
        public TValue Value
        {
            get { return _value; }
            set { _value = value; }
        }


        public NodeItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public virtual void Dispose()
        {
            Dispose(ref _key);
            Dispose(ref _value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Dispose<T>(ref T item)
        {
            var disp = item as IDisposable;

            if (disp != null)
                disp.Dispose();

            item = default(T);
        }
    }
}
