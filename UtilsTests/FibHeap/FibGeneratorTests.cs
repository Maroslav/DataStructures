using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.DataStructures;
using Utils.DataStructures.Nodes;

namespace UtilsTests.FibHeap
{
    [TestClass]
    public class FibGeneratorTests
        : GeneratorTestsBase<Tuple<Stack<byte>, Stack<int>>>
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

        private const int ConsumerCount = 4;
        private int _currentJobsDone;

        private Stack<byte> _currentCommands = new Stack<byte>(BuilderInitSize);
        private Stack<int> _currentArguments = new Stack<int>(BuilderInitSize);

        private readonly SplayTree<int, float> _results = new SplayTree<int, float>();

        #endregion

        #region Genesis

        public FibGeneratorTests(string logFolderName)
            : base(logFolderName, LogFileName, ConsumerCount)
        {
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
                    var commands = _currentCommands;
                    var arguments = _currentArguments;

                    if (commands != null)
                    {
                        Buffer.Add(new Tuple<Stack<byte>, Stack<int>>(commands, arguments));

                        _currentCommands = new Stack<byte>(commands.Count + Math.Min(commands.Count, BuilderSizeIncrement));
                        _currentArguments = new Stack<int>(commands.Count * 2);
                    }

                    // Store the command count -- start gathering a new batch
                    _currentArguments.Push(int.Parse(data.Substring(2)));
                    return;
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

        private void ProcessData(Tuple<Stack<byte>, Stack<int>> data)
        {
            var sw = Stopwatch.StartNew();

            var commands = data.Item1;
            var arguments = data.Item2;

            int argOffset = 0;

            // Prepare the heap
            var heap = new FibonacciHeap<int, int>();
            int insertCount = arguments[argOffset++];
            NodeItem<int, int>[] insertedNodes = new NodeItem<int, int>[insertCount];
            float deleteDepthCount = 0;

            // Do insert commands
            foreach (byte command in commands)
            {
                int id, key;
                NodeItem<int, int> node;

                switch (command)
                {
                    case InsKey:
                        id = arguments[argOffset++];
                        key = arguments[argOffset++];
                        node = heap.Add(key, id);

                        Debug.Assert(insertedNodes[id] == null);
                        insertedNodes[id] = node;
                        break;

                    case DelKey:
                        node = heap.PeekMin();
                        insertedNodes[node.Value] = null;

                        heap.DeleteMin();
                        //deleteDepthCount += heap.LastSplayDepth / (float)(Math.Log10(heap.Count + 1) * 3.321928);
                        break;

                    case DecKey:
                        id = arguments[argOffset++];
                        key = arguments[argOffset++];
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

            float avgInsertDepth = deleteDepthCount / insertCount;

            lock (_results)
                _results.Add(insertCount, avgInsertDepth);

            Interlocked.Increment(ref _currentJobsDone);
            Log("{0}/{1} done/waiting :: {2:F} sec :: {3}/{4} adds/finds : {5:F}/{6:F} insert depth factor/find depth",
                _currentJobsDone,
                Buffer.WaitingItemCount,
                sw.ElapsedMilliseconds * 0.001
                //insertCount,
                //findCount,
                //avgInsertDepth,
                //avgFindDepth
                );
        }

        private void Finished()
        {
            if (_currentCommands != null)
            {
                Buffer.Add(new Tuple<Stack<byte>, Stack<int>>(_currentCommands, _currentArguments));
                _currentCommands = null;
            }

            string result = _results.Items.ToString(n => '\n' + n.Key.ToString() + ':' + n.Value.ToString());
            Log("\nResults:\n" + result + '\n');
        }

        #endregion

        #region Running

        public Task RunGeneratorSubset(int t)
        {
            Debug.Assert(t > 0);

            var pars = string.Format("-s {0} -t {1}", Seed, t);
            return Generator.RunGenerator(Path.Combine(ToolsPath, GeneratorName), pars, GenerateHandler);
        }

        private void RunInternal(string runName, Task generatorTask)
        {
            StartConsumers(ProcessData);
            Run(runName, generatorTask);
        }

        public void RunSubset(int T)
        {
            var generatorTask = RunGeneratorSubset(T);
            RunInternal("T_test_" + T, generatorTask);
        }

        #endregion
    }
}
