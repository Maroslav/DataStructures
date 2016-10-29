﻿using System;
using System.Collections.Generic;
using Utils.DataStructures;

namespace ExternalSortRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            SplayTree<int, string> d = new SplayTree<int, string>();

            for (int i = 0; i < 10; i++)
                d[i] = i.ToString();


            foreach (var n in d)
            {
                Console.WriteLine(string.Format("{0}\t{1}", n.Key, n.Value));
            }

            ((ICollection<int>)d.Keys).Clear();
        }
    }
}
