using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.Other;

namespace UtilsTests.Helpers
{
    public class MyCommandGenerator
    {
        #region Fields, properties and constants

        private const int TimeOut = 6000000; // 6000 seconds = 100 minutes timeout

        private string FolderPath
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        private readonly CancellationTokenSource _cancellationTokenSource;

        public ProcessHelper Process { get; private set; }

        #endregion

        #region Genesis

        public MyCommandGenerator(CancellationTokenSource cts)
        {
            _cancellationTokenSource = cts;
        }

        #endregion

        #region Generating

        public async Task RunGenerator(string generatorPath, string pars, Action<string> dataReceivedHandler, bool redirectStdIn = false)
        {
            using (Process = new ProcessHelper(
                Console.WriteLine,
                (sender, eventArgs) => dataReceivedHandler(eventArgs.Data),
                //(sender, args) => Console.WriteLine("Error: " + args.Data),
                (sender, args) => Thread.CurrentThread.Abort(),
                null))
            {
                Process.StartProcess(
                    generatorPath,
                    pars,
                    FolderPath,
                    _cancellationTokenSource.Token,
                    redirectStdIn);

                var result = await Process.Wait(TimeOut).ConfigureAwait(false);

                if (result != WaitResult.Ok)
                    _cancellationTokenSource.Cancel();

                Assert.IsTrue(result.HasFlag(WaitResult.Ok), "Generator process error: " + result);
                Console.WriteLine("Generator finished. Running time: " + Process.GetElapsedTime());
            }
        }

        #endregion
    }
}
