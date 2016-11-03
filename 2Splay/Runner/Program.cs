using System;
using System.Collections.Generic;
using Utils.DataStructures.SplayTree;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var generator = new GeneratorWrapper.OpGeneratorWrapper();

            generator.Generate(new [] { "generator", "-s 82", "-t 100000"});
        }
    }
}
