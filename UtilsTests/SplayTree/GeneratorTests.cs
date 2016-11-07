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
            _cancellationTokenSource.Token.Register(() => Console.WriteLine("Cancellation requested."));
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
            Console.Write(_currentJobsTaken);
            Console.Write(':');
            Console.Write(_buffer.WaitingItemCount);

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

            int avgDepth = depthSum / findCount;

            lock (_results)
                _results.Add(insertCount, avgDepth);

            Console.Write("::");
            Console.Write(insertCount);
            Console.Write(":");
            Console.Write(avgDepth);
            Console.Write(' ');
        }

        #endregion


        public void FinishRun()
        {
            foreach (var c in _consumers)
                c.ExitWhenNoItems = true;

            Task.WaitAll(_consumers.Select(c => c.RequestCollectionTask).ToArray(), _cancellationTokenSource.Token);

            Console.WriteLine(_results.Items.ToString(n => n.Key.ToString() + ':' + n.Value.ToString()));
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
