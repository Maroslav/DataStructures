using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ExternalSorter.RunData
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Entry
    {
        #region IComparer<T>

        public class EntryComparer : IComparer<Entry>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(Entry x, Entry y)
            {
                long val1 = x.Value;
                long val2 = y.Value;

                if (val1 < val2)
                    return -1;
                if (val2 > val1)
                    return 1;

                return 0;
            }
        }

#endregion


        [FieldOffset(0)]
        public long Value1;

        [FieldOffset(8)]
        public int Row1;


        // Value is stored in the first 63 bits
        public long Value
        {
            get { return Value1 >> 1; }
        }

        // Row index is stored in the last 33 bits
        public long Row
        {
            get
            {
                return
                    (Value1 & 1) << 32
                    | Row1;
            }
        }
    }
}
