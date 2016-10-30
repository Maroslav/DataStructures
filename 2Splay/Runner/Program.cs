using System;
using System.Collections.Generic;
using Utils.DataStructures.SplayTree;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            SplayTree<int, string> d = new SplayTree<int, string>();

            Random rnd = new Random(1);

            for (int i = 0; i < 10; i++)
            {
                int r = rnd.Next();
                d[r] = r.ToString();
            }


            //foreach (var n in d)
            //{
            //    Console.WriteLine(string.Format("{0}\t{1}", n.Key, n.Value));
            //}

            ((ICollection<int>)d.Keys).Clear();
        }
    }
}
