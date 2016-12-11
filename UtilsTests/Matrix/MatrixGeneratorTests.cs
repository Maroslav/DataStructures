using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.Structures;
using UtilsTests.FibHeap;
using UtilsTests.Helpers;

namespace UtilsTests.Matrix
{
    [TestClass]
    public class MatrixGeneratorTests
    {
#if NONCLEAN
        private const string GeneratorName = "CacheSimulator.exe";

        private CancellationTokenSource _cts = new CancellationTokenSource();


        public void TestCache(int matrixSize, int blockSize, int blockCount)
        {
            //Matrix<int> mm = new Matrix<int>(s => { }, matrixSize, matrixSize);
            //mm.Transpose();
            //return;

            var cacheProcess = new MyCommandGenerator(_cts);

            string pars = string.Format("{0} {1}", blockSize, blockCount);
            var generatorTask = cacheProcess.RunGenerator(Path.Combine(GeneratorTestsBase<object>.ToolsPath, GeneratorName), pars, Log, true);

            Matrix<int> m = new Matrix<int>(cacheProcess.Process.WriteLineToStdIn, matrixSize, matrixSize);
            m.Transpose();
            cacheProcess.Process.WriteLineToStdIn("\0"); // Terminate the program

            generatorTask.Wait(20000, _cts.Token); // Wait for 20 sec
        }


        private void Log(string message)
        {
#if DEBUG
            Console.WriteLine(message);
#endif
        }

#if LONG_RUNNING_TESTS
        [TestMethod]
#endif
        public void TestCache()
        {
            TestCache(5, 64, 64);
        }
#endif // NONCLEAN

#if LONG_RUNNING_TESTS
        [TestMethod]
#endif
        public void TestTime()
        {

        }
    }
}
