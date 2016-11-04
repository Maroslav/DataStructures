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

        private const int TimeOut = 600000; // 600 seconds timeout
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

        private CommandState _state = CommandState.Init;

        private readonly AsyncBuffer<StringBuilder> _buffer;
        private readonly CancellationTokenSource _cancellationTokenSource;
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
                Assert.Fail();
            }
        }

        public void RunGenerator(int t = -1)
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

                var result = generatorProcess.Wait(TimeOut);

                if (result != WaitResult.Ok)
                    _cancellationTokenSource.Cancel();

                Assert.IsTrue(result.HasFlag(WaitResult.Ok), "Generator process error: " + result);
            }
        }

        #endregion
    }

    class MyCommandConsumer
        : IDisposable
    {
        private readonly AsyncBuffer<StringBuilder> _buffer;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Action<StringBuilder> _callback;

        private Task _requestCollectionTask;


        public MyCommandConsumer(AsyncBuffer<StringBuilder> buffer, CancellationTokenSource cts)
        {
            _buffer = buffer;
            _cancellationTokenSource = cts;
        }

        public void Start(Action<StringBuilder> callback)
        {
            _requestCollectionTask = Task.Factory.StartNew(RunRequestCollectionAsync, TaskCreationOptions.LongRunning);
            _callback = callback;
        }

        public void Dispose()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource.Cancel();
        }


        #region Long-running method

        private void RunRequestCollectionAsync()
        {
            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    Task<StringBuilder> task;

                    // Get a new task to wait for
                    try
                    {
                        task = _buffer.Get();
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
                        task.Wait(_cancellationTokenSource.Token);
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
            finally
            {
                _buffer.Clear();
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

        private readonly MyCommandGenerator _generator;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly SplayTree<int, int> _results = new SplayTree<int, int>();

        #endregion

        #region Genesis

        public GeneratorTests()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var buffer = new AsyncBuffer<StringBuilder>(_cancellationTokenSource.Token);
            _generator = new MyCommandGenerator(buffer, _cancellationTokenSource);

            MyCommandConsumer[] consumers = Enumerable
                .Range(0, ConsumerCount)
                .Select(n => new MyCommandConsumer(buffer, _cancellationTokenSource))
                .ToArray();

            foreach (var consumer in consumers)
                consumer.Start(Process);
        }

        public void Dispose()
        {
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
            int offset = 0;

            // Prepare the tree
            int insertCount = ParseNextNumber(commands, ref offset);
            var tree = new SplayTree<int, string>();

            int complexity = 0; // TODO

            // Do insert commands
            for (int i = 0; i < insertCount; i++)
            {
                int key = ParseNextNumber(commands, ref offset);
                tree.Add(key, null);
            }

            // Do find commands
            while (offset < commands.Length)
            {
                int key = ParseNextNumber(commands, ref offset);
                string val = tree[key];
            }

            // Cleanup and store the measurements
            tree.Clear();

            lock (_results)
                _results.Add(insertCount, complexity);
        }

        #endregion


        [TestMethod]
        public void Run()
        {
            _generator.RunGenerator(100);
            Debug.WriteLine(_results.Items);
        }
    }
}
