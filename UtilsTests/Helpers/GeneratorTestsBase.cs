using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.DataStructures;
using UtilsTests.Helpers;

namespace UtilsTests.FibHeap
{
    [TestClass]
    public abstract class GeneratorTestsBase<TWorkItem>
        : IDisposable
    {
        #region Fields and constants

        public const string ToolsPath = @"Tools";

        private readonly string _logFolderName;
        private readonly string _logFileName;

        protected readonly CancellationTokenSource CancellationTokenSource;
        protected readonly AsyncBuffer<TWorkItem> Buffer;

        protected readonly MyCommandGenerator Generator;
        protected readonly MyCommandConsumer<TWorkItem>[] Consumers;

        private TextWriter _log;
        protected event Action OnGeneratorFinished;
        protected event Action OnFinished;

        #endregion

        #region Genesis

        public GeneratorTestsBase(string logFolderName, string logFileName, int consumerCount)
        {
            _logFolderName = logFolderName ?? @"Log";
            _logFileName = logFileName;

            CancellationTokenSource = new CancellationTokenSource();
            CancellationTokenSource.Token.Register(() => Log("Cancellation requested."));
            Buffer = new AsyncBuffer<TWorkItem>(CancellationTokenSource.Token);

            Generator = new MyCommandGenerator(CancellationTokenSource);
            Consumers = Enumerable
                .Range(0, consumerCount)
                .Select(n => new MyCommandConsumer<TWorkItem>(Buffer, CancellationTokenSource))
                .ToArray();
        }

        public virtual void Dispose()
        {
            if (!CancellationTokenSource.IsCancellationRequested)
                CancellationTokenSource.Cancel();
        }

        #endregion

        #region Logging

        protected void Log(string message)
        {
            if (_log != null)
                _log.WriteLine(message);

            Console.WriteLine(message);
        }

        protected void Log(string format, params object[] args)
        {
            if (_log != null)
                _log.WriteLine(format, args);

            Console.WriteLine(format, args);
        }

        #endregion

        #region Running

        private async Task HandleGeneratorDone(Task generatorTask)
        {
            await generatorTask.ConfigureAwait(false);

            if (OnGeneratorFinished != null)
                OnGeneratorFinished();

            // Signal the finish to consumers
            foreach (var c in Consumers)
                c.ExitWhenNoItems = true;
        }

        private void FinishRun(Task generatorTask)
        {
            HandleGeneratorDone(generatorTask);

            var consumers = Consumers.Select(c => c.RequestCollectionTask).ToArray();

            while (!Task.WaitAll(consumers, 2000, CancellationTokenSource.Token))
            {
                // Check for user cancellation
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    Log("Terminating...");
                    CancellationTokenSource.Cancel();
                    return;
                }
            }

            if (OnFinished != null)
                OnFinished();

            Assert.IsFalse(CancellationTokenSource.IsCancellationRequested);
        }

        protected void Run(string runName, Task generatorTask)
        {
            if (!Directory.Exists(_logFolderName))
                Directory.CreateDirectory(_logFolderName);

            using (
                _log = TextWriter.Synchronized(new StreamWriter(Path.Combine(_logFolderName, _logFileName) + '_' + runName + ".txt")))
            {
                Log("\n---- Running " + runName);
                var sw = Stopwatch.StartNew();

                FinishRun(generatorTask);

                sw.Stop();
                Log("---- " + runName + " finished in: " + sw.Elapsed);
            }

            _log = null;
        }

        protected void StartConsumers(Action<TWorkItem> processingAction)
        {
            Console.WriteLine();
            Console.WriteLine("Starting " + Consumers.Length + " workers.");
            for (int i = 0; i < Consumers.Length; i++)
            {
                var consumer = Consumers[i];
                consumer.Start(processingAction);

                int closure = i;
                consumer.RequestCollectionTask.ContinueWith(
                    task => Console.WriteLine("Worker " + closure + " finished."));
            }
        }

        #endregion
    }
}
