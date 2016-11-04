using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeneratorWrapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UtilsTests.SplayTree
{
    [TestClass]
    public class GeneratorTests
    {
        private OpGeneratorWrapper _generator = new OpGeneratorWrapper();


        private Stream RunGenerator(int t = -1)
        {
            var stream = new MemoryStream();

            using (var writer = new StreamWriter("test.txt"))
            {
                var pars = new[]
                {
                    "generator",
                    "-s 82",
                    t == -1
                        ? "-b"
                        : "-t " + t,
                };

                // Redirect the console output to our stream
                Console.SetOut(writer);
                _generator.Generate(pars);
            }

            return stream;
        }


        [TestMethod]
        public void Run()
        {
            var ops = RunGenerator(10);
            Debug.WriteLine(ops.Length);
        }
    }
}
