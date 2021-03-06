﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.Structures;
using UtilsTests.FibHeap;
using UtilsTests.Helpers;

namespace UtilsTests.Matrix
{
    [TestClass]
    public class MatrixGeneratorTests
    {
        #region Fields and constants

        private const string GeneratorName = "CacheSimulator.exe";
        private const string LogFileName = "FibHeapLog";

        private readonly int[] _blockSizes = { 64, 64, 64, 512, 4096 };
        private readonly int[] _blockCounts = { 64, 1024, 4096, 512, 64 };


        private readonly string _logFolderName;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private StreamWriter _log;

        public CancellationToken CancellationToken { get { return _cts.Token; } }

        #endregion

        #region Genesis

        public MatrixGeneratorTests(string logFolderName = null)
        {
            _logFolderName = logFolderName ?? @"Log";

            if (!Directory.Exists(_logFolderName))
                Directory.CreateDirectory(_logFolderName);
        }

        #endregion

        #region Logging

        private void LogLine(string message = null)
        {
            _log.WriteLine(message);
            Console.WriteLine(message);
        }

        private void LogLine(string format, params object[] arg)
        {
            LogLine(string.Format(format, arg));
        }

        private void Log(string message)
        {
            _log.Write(message);
            Console.Write(message);
        }

        private void Log(string format, params object[] arg)
        {
            Log(string.Format(format, arg));
        }

        #endregion

#if NONCLEAN
        #region Cache testing

        private void LogFilteredSimOutput(int matrixSize, string message)
        {
            if (message == null)
                return;

            const string missesPrefix = "Misses: ";

            if (!message.StartsWith(missesPrefix))
                return;

            int swapCount = (matrixSize * matrixSize - matrixSize) / 2;
            int misses = int.Parse(message.Substring(missesPrefix.Length));
            LogLine("{0}::{1}", matrixSize, misses / (float)swapCount);
        }

        public void TestCache(int matrixSize, int blockSize, int blockCount)
        {
            var cacheProcess = new MyCommandGenerator(_cts);
            Action<string> cacheSimMessageCallback = s => LogFilteredSimOutput(matrixSize, s);

            // TODO: Run the cache sim once for all the test...
            var generatorTask = cacheProcess.RunGenerator(
                Path.Combine(GeneratorTestsBase<object>.ToolsPath, GeneratorName),
                string.Format("{0} {1}", blockSize, blockCount),
                cacheSimMessageCallback,
                true, false);

            int swapCount = 0;

            Action<string> matrixOutputHandler = s =>
            {
                swapCount++;
                cacheProcess.Process.WriteLineToStdIn(s);
            };

            Matrix<int> m = new Matrix<int>(matrixOutputHandler, matrixSize, matrixSize);
            m.Transpose();
            cacheProcess.Process.WriteLineToStdIn("\0"); // Terminate the program

            swapCount -= 2; // There were two more logs -- N and E
            Assert.AreEqual(swapCount, (matrixSize * matrixSize - matrixSize) / 2);

            generatorTask.Wait(20000, _cts.Token); // Wait for 20 sec
        }

        IEnumerable<int> GetMatrixSizes()
        {
            int k = 54;

            while (k <= 13 * 9) // Run up to 256MB matrix size
            //while (k <= 14 * 9) // Run up to 1GB matrix size
            {
                int size = (int)Math.Pow(2, k++ / 9f);
                yield return size;
            }
        }

        public void TestCache(int i)
        {
            int blockSize = _blockSizes[i];
            int blockCount = _blockCounts[i];

            string suffix = string.Format("_b-{0}_c-{1}", blockSize, blockCount);
            using (_log = new StreamWriter(Path.Combine(_logFolderName, LogFileName) + suffix + ".txt"))
            {
                Console.WriteLine("\nStarting BlockSize/BlockCount: {0}/{1}", blockSize, blockCount);
                LogLine("Size::miss");

                foreach (var matrixSize in GetMatrixSizes())
                    TestCache(matrixSize, blockSize, blockCount);

                LogLine();
                Console.WriteLine("\nFinished {0}/{1}\n", blockSize, blockCount);
            }
        }

        #endregion
#endif // NONCLEAN


#if LONG_RUNNING_TESTS
        [TestMethod]
#endif
        public void TestCacheFirstHalf()
        {
#if NONCLEAN
            for (int i = 0; i < _blockSizes.Length / 2; i++)
                TestCache(i);
#else
            Assert.Fail("Nothing to test in clean mode...");
#endif // NONCLEAN

        }

#if LONG_RUNNING_TESTS
        [TestMethod]
#endif
        public void TestCacheSecondHalf()
        {
#if NONCLEAN
            for (int i = _blockSizes.Length / 2; i < _blockSizes.Length; i++)
                TestCache(i);
#else
            Assert.Fail("Nothing to test in clean mode...");
#endif // NONCLEAN
        }


#if LONG_RUNNING_TESTS
        [TestMethod]
#endif
        public void TestTime()
        {
            // TODO: move to another test class
        }
    }
}
