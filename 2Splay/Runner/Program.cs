using System.Collections.Generic;

namespace ExternalSortRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<int, string> d = new Dictionary<int, string>();

            for (int i = 0; i < 10; i++)
                d[i] = i.ToString();

            ((ICollection<int>) d.Keys).Clear();
        }
    }
}
