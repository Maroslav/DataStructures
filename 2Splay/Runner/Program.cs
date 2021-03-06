﻿using UtilsTests.SplayTree;

namespace Runner
{
    class Program
    {
        private const string LogFolderName = "Log";

        static void Main(string[] args)
        {
            int[] ts = { 10, 100, 1000, 10000, 100000, 1000000 };

            foreach (var t in ts)
            {
                var test = new GeneratorTests(LogFolderName);
                test.RunSubset(t);
                test.Dispose();
            }

            {
                var test = new GeneratorTests(LogFolderName);
                test.RunSequential();
                test.Dispose();
            }
        }
    }
}
