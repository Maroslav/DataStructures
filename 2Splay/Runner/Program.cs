using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilsTests.SplayTree;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            GeneratorTests tests = new GeneratorTests();

            tests.Run100T();
        }
    }
}
