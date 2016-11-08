using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.DataStructures;

namespace UtilsTests.SplayTree
{
    [TestClass]
    public class GeneratorTests
        : IDisposable
    {
        #region Fields and constants

        private readonly string _logFolderName;
        private const string LogFileName = "SplayTreeLog";

        private const int ConsumerCount = 4;
        private int _currentJobsDone;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly AsyncBuffer<Stack<int>> _buffer;

        private readonly MyCommandGenerator _generator;
        private readonly MyCommandConsumer[] _consumers;

        private readonly SplayTree<int, float> _results = new SplayTree<int, float>();
        private TextWriter _log;

        #endregion

        #region Genesis

        public GeneratorTests(string logFolderName)
        {
            _logFolderName = logFolderName;

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Token.Register(() => Log("Cancellation requested."));
            _buffer = new AsyncBuffer<Stack<int>>(_cancellationTokenSource.Token);

            _generator = new MyCommandGenerator(_buffer, _cancellationTokenSource);
            _consumers = Enumerable
                .Range(0, ConsumerCount)
                .Select(n => new MyCommandConsumer(_buffer, _cancellationTokenSource))
                .ToArray();

            for (int i = 0; i < _consumers.Length; i++)
            {
                var consumer = _consumers[i];
                consumer.Start(Process);

                int closure = i;
                consumer.RequestCollectionTask.ContinueWith(task => Console.WriteLine("Worker " + closure + " finished."));
            }
        }

        public void Dispose()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource.Cancel();
        }

        #endregion

        #region Processing

        private void Process(Stack<int> commands)
        {
            var sw = Stopwatch.StartNew();

            int offset = 0;

            // Prepare the tree
            var tree = new SplayTree<int, string>();
            int insertCount = commands[offset++];
            float insertDepthSum = 0;

            // Do insert commands
            for (; offset < insertCount + 1; offset++)
            {
                int key = commands[offset];
                tree.Add(key, null);

                insertDepthSum += tree.LastSplayDepth / (float)(Math.Log10(tree.Count + 1) * 3.321928);
                // Normalized by the expected depth of a balanced binary tree; Plus one for when Count==1
            }

            var findDepthSum = 0;
            int findCount = 0;
            offset--;

            // Do find commands
            while (++offset < commands.Count)
            {
                int key = commands[offset];
                string val = tree[key];

                findDepthSum += tree.LastSplayDepth;
                findCount++;
            }

            // Cleanup and store the measurements
            //tree.Clear(); // Disassembles tree node pointers .... not a good idea with a GC...

            sw.Stop();

            float avgInsertDepth = insertDepthSum / insertCount;
            float avgFindDepth = findDepthSum / (float)findCount;

            lock (_results)
                _results.Add(insertCount, avgFindDepth);

            Interlocked.Increment(ref _currentJobsDone);
            Log("{0}/{1} done/waiting :: {2:F} sec :: {3}/{4} adds/finds : {5:F}/{6:F} insert depth factor/find depth",
                _currentJobsDone,
                _buffer.WaitingItemCount,
                sw.ElapsedMilliseconds * 0.001,
                insertCount,
                findCount,
                avgInsertDepth,
                avgFindDepth);
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_log != null)
                _log.WriteLine(message);

            Console.WriteLine(message);
        }

        private void Log(string format, params object[] args)
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

            // Signal the finish to consumers
            foreach (var c in _consumers)
                c.ExitWhenNoItems = true;
        }

        private void FinishRun(Task generatorTask)
        {
            HandleGeneratorDone(generatorTask);

            var consumers = _consumers.Select(c => c.RequestCollectionTask).ToArray();

            while (!Task.WaitAll(consumers, 2000, _cancellationTokenSource.Token))
            {
                // Check for user cancellation
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    Log("Terminating...");
                    _cancellationTokenSource.Cancel();
                    return;
                }
            }

            string result = _results.Items.ToString(n => n.Key.ToString() + ':' + n.Value.ToString());
            Log("\nResults:\n" + result + '\n');

            Assert.IsFalse(_cancellationTokenSource.IsCancellationRequested);
        }

        private void Run(string runName, Task generatorTask)
        {
            using (_log = TextWriter.Synchronized(new StreamWriter(Path.Combine(_logFolderName, LogFileName) + '_' + runName)))
            {
                Log("\n---- Running " + runName);
                var sw = Stopwatch.StartNew();

                FinishRun(generatorTask);

                sw.Stop();
                Log("---- " + runName + " finished in: " + sw.Elapsed);
            }

            _log = null;
        }

        public void RunSubset(int T)
        {
            var generatorTask = _generator.RunGeneratorSubset(T);
            Run("T_test_" + T, generatorTask);
        }

        private void RunSeq()
        {
            var generatorTask = _generator.RunGeneratorSequential();
            Run("Seq_test", generatorTask);
        }

        #endregion


        [TestMethod]
        public void Run10T()
        {
            RunSubset(10);
        }

        [TestMethod]
        public void Run100T()
        {
            RunSubset(100);
        }

        [TestMethod]
        public void Run1000T()
        {
            RunSubset(1000);
        }

        [TestMethod]
        public void Run10000T()
        {
            RunSubset(10000);
        }

        [TestMethod]
        public void Run100000T()
        {
            RunSubset(100000);
        }

        [TestMethod]
        public void Run1000000T()
        {
            RunSubset(1000000);
        }


        [TestMethod]
        public void RunSequential()
        {
            RunSeq();
        }
    }
}
