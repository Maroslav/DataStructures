using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.DataStructures;
using Utils.Other;

namespace UtilsTests.SplayTree
{
    class MyCommandGenerator
    {
        private enum CommandState
        {
            Init,
            Starts,
            Inserts,
            Finds,
        }


        #region Constants

        private const int TimeOut = 6000000; // 6000 seconds = 100 minutes timeout
        private const int BuilderInitSize = 1000;
        private const int BuilderSizeIncrement = 3000;

        private const string ToolsPath = @"Tools";
        private const string GeneratorName = "generator.exe";

        private string FolderPath
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        #endregion

        #region Fields

        private readonly AsyncBuffer<StringBuilder> _buffer;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private CommandState _state = CommandState.Init;
        private StringBuilder _currentBuilder = new StringBuilder(BuilderInitSize);

        #endregion

        #region Genesis

        public MyCommandGenerator(AsyncBuffer<StringBuilder> buffer, CancellationTokenSource cts)
        {
            _buffer = buffer;
            _cancellationTokenSource = cts;
        }

        #endregion

        #region Generating

        private void DataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            try
            {
                switch (e.Data[0])
                {
                    case '#':
                        Debug.Assert(_state == CommandState.Init || _state == CommandState.Finds); // A race condition -- another DataReceivedHandler changed the state

                        // Replace the builder by a new instance
                        StringBuilder builder = _currentBuilder;

                        if (_state != CommandState.Init)
                        {
                            _buffer.Add(builder);

                            _currentBuilder = new StringBuilder(builder.Length + BuilderSizeIncrement);
                        }

                        // Start gathering a new batch
                        _currentBuilder.Append(e.Data.Substring(2));
                        _currentBuilder.Append(' ');
                        _state = CommandState.Starts;
                        return;

                    case 'I':
                        Debug.Assert(_state == CommandState.Inserts || _state == CommandState.Starts); // A race condition -- another DataReceivedHandler changed the state

                        if (_state == CommandState.Starts)
                            _state = CommandState.Inserts;

                        _currentBuilder.Append(e.Data.Substring(2));
                        _currentBuilder.Append(' ');
                        break;

                    case 'F':
                        Debug.Assert(_state == CommandState.Finds || _state == CommandState.Inserts); // A race condition -- another DataReceivedHandler changed the state

                        if (_state == CommandState.Inserts)
                            _state = CommandState.Finds;

                        _currentBuilder.Append(e.Data.Substring(2));
                        _currentBuilder.Append(' ');
                        break;
                }
            }
            catch (Exception)
            {
                _cancellationTokenSource.Cancel();
                Assert.Fail("Error in parsing generator data.");
            }
        }

        public async Task RunGenerator(int t = -1)
        {
            using (var generatorProcess = new ProcessHelper(
                s => Debug.WriteLine(s),
                DataReceivedHandler,
                (sender, args) => Thread.CurrentThread.Abort(),
                null))
            {
                var pars =
                    "-s 82 " +
                    (t <= 0
                        ? "-b"
                        : "-t " + t);

                generatorProcess.StartProcess(Path.Combine(ToolsPath, GeneratorName), pars, FolderPath);

                var result = await generatorProcess.Wait(TimeOut);

                if (result != WaitResult.Ok)
                    _cancellationTokenSource.Cancel();

                Assert.IsTrue(result.HasFlag(WaitResult.Ok), "Generator process error: " + result);
                Debug.WriteLine("Generator finished. Running time: " + generatorProcess.GetElapsedTime());
            }
        }

        #endregion
    }

    class MyCommandConsumer
    {
        private readonly AsyncBuffer<StringBuilder> _buffer;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Action<StringBuilder> _callback;

        public Task RequestCollectionTask;
        public bool ExitWhenNoItems;


        public MyCommandConsumer(AsyncBuffer<StringBuilder> buffer, CancellationTokenSource cts)
        {
            _buffer = buffer;
            _cancellationTokenSource = cts;
        }

        public void Start(Action<StringBuilder> callback)
        {
            _callback = callback;
            RequestCollectionTask = Task.Factory.StartNew(
                RunRequestCollectionAsync,
                _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }


        #region Long-running method

        private void RunRequestCollectionAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                Task<StringBuilder> task;

                // Get a new task to wait for
                try
                {
                    bool got = _buffer.TryGet(out task);

                    if (ExitWhenNoItems && !got)
                        // The producer has finished his work -- if there are no more waiting items, we can safely exit
                        return;
                }
                catch (ObjectDisposedException)
                {
                    Debug.Assert(_cancellationTokenSource.IsCancellationRequested);
                    return;
                }
                catch (Exception)
                {
                    // Should not ever happen ;-)
                    Debug.Assert(false);
                    continue;
                }

                // Wait for the producers to queue up an item
                try
                {
                    while (!task.Wait(1000, _cancellationTokenSource.Token))
                    {
                        if (ExitWhenNoItems)
                            // The producer has finished his work -- no new items will ever be created and we can safely exit
                            return;
                    }
                }
                catch (Exception)
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;

                    continue;
                }

                // Pass the item back
                StringBuilder result = task.Result; // Can throw if cancellation was requested (we want to end the thread anyway)
                _callback(result);
            }
        }

        #endregion
    }

    [TestClass]
    public class GeneratorTests
        : IDisposable
    {
        #region Fields and constants

        private const int ConsumerCount = 4;
        private int _currentJobsTaken;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly AsyncBuffer<StringBuilder> _buffer;

        private readonly MyCommandGenerator _generator;
        private readonly MyCommandConsumer[] _consumers;

        private readonly SplayTree<int, int> _results = new SplayTree<int, int>();

        #endregion

        #region Genesis

        public GeneratorTests()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Token.Register(() => Debug.WriteLine("Cancellation requested."));
            _buffer = new AsyncBuffer<StringBuilder>(_cancellationTokenSource.Token);

            _generator = new MyCommandGenerator(_buffer, _cancellationTokenSource);
            _consumers = Enumerable
                .Range(0, ConsumerCount)
                .Select(n => new MyCommandConsumer(_buffer, _cancellationTokenSource))
                .ToArray();

            foreach (var consumer in _consumers)
                consumer.Start(Process);
        }

        public void Dispose()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource.Cancel();
        }

        #endregion

        #region Processing

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsNumber(char c)
        {
            return c >= '0' && c <= '9';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ParseNextNumber(StringBuilder builder, ref int offset)
        {
            char c;
            int num = 0;

            while (IsNumber(c = builder[offset++]))
            {
                num *= 10;
                num += c - '0';
            }

            return num;
        }

        private void Process(StringBuilder commands)
        {
            Interlocked.Increment(ref _currentJobsTaken);
            //Debug.Write(_currentJobsTaken);
            //Debug.Write(':');
            //Debug.Write(_buffer.WaitingItemCount);
            //Debug.Write(' ');

            int offset = 0;

            // Prepare the tree
            int insertCount = ParseNextNumber(commands, ref offset);
            var tree = new SplayTree<int, string>();

            // Do insert commands
            for (int i = 0; i < insertCount; i++)
            {
                int key = ParseNextNumber(commands, ref offset);
                tree.Add(key, null);
            }

            int depthSum = 0;
            int findCount = 0;

            // Do find commands
            while (offset < commands.Length)
            {
                int key = ParseNextNumber(commands, ref offset);
                string val = tree[key];

                depthSum += tree.LastSplayDepth;
                findCount++;
            }

            // Cleanup and store the measurements
            tree.Clear();

            lock (_results)
                _results.Add(insertCount, depthSum / findCount);
        }

        #endregion


        public void FinishRun()
        {
            foreach (var c in _consumers)
                c.ExitWhenNoItems = true;

            Task.WaitAll(_consumers.Select(c => c.RequestCollectionTask).ToArray(), _cancellationTokenSource.Token);

            Debug.WriteLine(_results.Items.ToString(n => n.Key.ToString() + ':' + n.Value.ToString()));
            Assert.IsFalse(_cancellationTokenSource.IsCancellationRequested);
        }

        private void RunT(int T)
        {
            _generator.RunGenerator(T).Wait();
            FinishRun();
        }


        [TestMethod]
        public void Run10T()
        {
            RunT(10);
        }

        [TestMethod]
        public void Run100T()
        {
            RunT(100);
        }

        [TestMethod]
        public void Run1000T()
        {
            RunT(1000);
        }

        [TestMethod]
        public void Run10000T()
        {
            RunT(10000);
        }

        [TestMethod]
        public void Run100000T()
        {
            RunT(100000);
        }

        [TestMethod]
        public void Run1000000T()
        {
            RunT(1000000);
        }
    }
}
