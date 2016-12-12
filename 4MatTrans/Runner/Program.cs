
using System;
using System.Threading;
using System.Threading.Tasks;
using UtilsTests.FibHeap;
using UtilsTests.Matrix;

namespace Runner
{
    class Program
    {
        private const string LogFolderName = "Log";

        static void Main(string[] args)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            Task first = Task.Run(() =>
            {
                var test = new MatrixGeneratorTests(LogFolderName);
                test.CancellationToken.Register(() => cts.Cancel()); // If the test fails, the cts will cancel the other one
                test.TestCacheFirstHalf();
            },
            cts.Token);

            Task second = Task.Run(() =>
            {
                var test = new MatrixGeneratorTests(LogFolderName);
                test.CancellationToken.Register(() => cts.Cancel()); // If the test fails, the cts will cancel the other one
                test.TestCacheSecondHalf();
            },
            cts.Token);

            while (!Task.WaitAll(new[] { first, second }, 2000))
            {
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("Terminating...");
                    cts.Cancel();
                    return;
                }
            }
        }
    }
}
