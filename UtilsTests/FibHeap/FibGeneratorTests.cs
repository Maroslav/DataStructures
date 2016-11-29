using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.DataStructures;
using Utils.DataStructures.Nodes;

namespace UtilsTests.FibHeap
{
    [TestClass]
    public class FibGeneratorTests
        : GeneratorTestsBase<Tuple<Stack<byte>, Stack<Stack<int>>>>
    {
        #region Fields and constants

        private const int BuilderInitSize = 120000;
        private const int BuilderSizeIncrement = 100000;
        private const int Seed = 82;

        private const byte InsKey = 2;
        private const byte DelKey = 3;
        private const byte DecKey = 4;

        private readonly char[] _splitArray = { ' ', '\n' };

        private const string GeneratorName = "FibHeapGenerator.exe";
        private const string LogFileName = "FibHeapLog";

        private const int ConsumerCount = 2;
        private int _currentJobsDone;

        private Stack<byte> _currentCommands = new Stack<byte>(BuilderInitSize);
        private Stack<Stack<int>> _currentArgList = new Stack<Stack<int>>();
        private Stack<int> _currentArguments = new Stack<int>(BuilderInitSize);

        private readonly SplayTree<int, float> _results = new SplayTree<int, float>();

        #endregion

        #region Genesis

        // Needed because of unit testing
        public FibGeneratorTests()
            : this(null)
        { }

        public FibGeneratorTests(string logFolderName)
            : base(logFolderName, LogFileName, ConsumerCount)
        {
            OnGeneratorFinished += GeneratorFinished;
            OnFinished += Finished;
            StartConsumers(ProcessData);
        }

        #endregion

        #region Processing

        private void GenerateHandler(string data)
        {
            if (data == null)
                return;

            try
            {
                if (data[0] == '#')
                {
                    // Replace the builders by new instances and sent the batch to consumers
                    if (_currentCommands.Count > 0)
                    {
                        _currentArgList.Push(_currentArguments);
                        Buffer.Add(new Tuple<Stack<byte>, Stack<Stack<int>>>(_currentCommands, _currentArgList));

                        _currentCommands = new Stack<byte>(_currentCommands.Count + Math.Min(_currentCommands.Count, BuilderSizeIncrement));
                        _currentArgList = new Stack<Stack<int>>();
                        _currentArguments = new Stack<int>(_currentCommands.Capacity * 2);
                    }

                    // Store the command count -- start gathering a new batch
                    _currentArguments.Push(int.Parse(data.Substring(2)));
                    return;
                }

                if (_currentArguments.Count / sizeof(int) > 100000000) // 100 MBs
                {
                    int sz = _currentArguments.Count;
                    _currentArgList.Push(_currentArguments);
                    _currentArguments = new Stack<int>(sz);
                }

                switch (data[2])
                {
                    case 'S': // INS
                        _currentCommands.Push(InsKey);
                        break;

                    case 'L': // DEL
                        _currentCommands.Push(DelKey);
                        return; // Don't try to push more data

                    case 'C': // DEC
                        _currentCommands.Push(DecKey);
                        break;
                }

                string[] line = data.Split(_splitArray, StringSplitOptions.RemoveEmptyEntries);
                Debug.Assert(line.Length == 3);
                _currentArguments.Push(int.Parse(line[1]));
                _currentArguments.Push(int.Parse(line[2]));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in parsing generator data:\n" + ex.Message);
                CancellationTokenSource.Cancel();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetNextArg(ref int offset, Stack<Stack<int>> argList, ref int listOffset)
        {
            var args = argList[listOffset];

            if (offset == args.Count)
            {
                args = argList[listOffset++];
                offset = 0;
            }

            return args[offset++];
        }

        private void ProcessData(Tuple<Stack<byte>, Stack<Stack<int>>> data)
        {
            var sw = Stopwatch.StartNew();

            var commands = data.Item1;
            var arguments = data.Item2;

            int argOffset = 0;
            int listOffset = 0;

            // Prepare the heap
            var heap = new FibonacciHeap<int, int>();
            int insertCount = GetNextArg(ref argOffset, arguments, ref listOffset);
            NodeItem<int, int>[] insertedNodes = new NodeItem<int, int>[insertCount];
            int deleteDepthCount = 0;
            int deleteCount = 0;

            // Do insert commands
            int id = 0, key = 0;
            NodeItem<int, int> node;

            foreach (byte command in commands)
            {
                if (command != DelKey)
                {
                    id = GetNextArg(ref argOffset, arguments, ref listOffset);
                    key = GetNextArg(ref argOffset, arguments, ref listOffset);
                }

                switch (command)
                {
                    case InsKey:
                        node = heap.Add(key, id);
                        Debug.Assert(insertedNodes[id] == null);
                        insertedNodes[id] = node;
                        break;

                    case DelKey:
                        node = heap.PeekMin();
                        Debug.Assert(insertedNodes[node.Value] != null);
                        insertedNodes[node.Value] = null;
                        heap.DeleteMin();

                        deleteDepthCount += heap.LastConsolidateDepth;
                        deleteCount++;
                        break;

                    case DecKey:
                        node = insertedNodes[id];

                        if (node == null || key > node.Key)
                            break;

                        heap.DecreaseKey(node, key);
                        break;
                }
            }

            // Cleanup and store the measurements
            //heap.Clear(); // Disassembles tree node pointers .... not a good idea with a GC...

            sw.Stop();

            float avgDeleteDepthCount = 0;

            if (deleteCount > 0)
                avgDeleteDepthCount = deleteDepthCount / (float)deleteCount;

            lock (_results)
                _results.Add(deleteCount, avgDeleteDepthCount);

            Interlocked.Increment(ref _currentJobsDone);
            Log("{0}/{1} done/waiting :: {2:F} sec :: {3} inserts :: {4}/{5:F} deletes/delete depth average",
                _currentJobsDone,
                Buffer.WaitingItemCount,
                sw.ElapsedMilliseconds * 0.001,
                insertCount,
                deleteCount,
                avgDeleteDepthCount);
        }

        private void GeneratorFinished()
        {
            if (_currentCommands != null)
            {
                _currentArgList.Push(_currentArguments);
                Buffer.Add(new Tuple<Stack<byte>, Stack<Stack<int>>>(_currentCommands, _currentArgList));
                _currentCommands = null;
            }
        }

        private void Finished()
        {
            string result = _results.Items.ToString(n => '\n' + n.Key.ToString() + ':' + n.Value.ToString());
            Log("\nResults:\n" + result + '\n');
        }

        #endregion

        #region Running

        public Task RunGenerator(string option)
        {
            var pars = string.Format("-s {0} {1}", Seed, option);
            return Generator.RunGenerator(Path.Combine(ToolsPath, GeneratorName), pars, GenerateHandler);
        }

        #endregion


        [TestMethod]
        public void BalancedTest()
        {
            var generatorTask = RunGenerator("-r");
            Run("Balanced_test", generatorTask);
        }

        [TestMethod]
        public void ImbalancedTest()
        {
            var generatorTask = RunGenerator("-b");
            Run("Imbalanced_test", generatorTask);
        }

        [TestMethod]
        public void MaliciousTest()
        {
            var generatorTask = RunGenerator("-x");
            Run("Malicious_test", generatorTask);
        }
    }
}
