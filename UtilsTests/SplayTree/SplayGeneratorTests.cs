using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.DataStructures;
using UtilsTests.FibHeap;

namespace UtilsTests.SplayTree
{
    [TestClass]
    public class SplayGeneratorTests
        : GeneratorTestsBase<Stack<int>>
    {
        #region Fields and constants

        private const int BuilderInitSize = 8000;
        private const int BuilderSizeIncrement = 3000;
        private const int Seed = 82;

        private const string GeneratorName = "generator.exe";
        private const string LogFileName = "SplayTreeLog";

        private const int ConsumerCount = 4;
        private int _currentJobsDone;

        private CommandState _state = CommandState.Init;
        private Stack<int> _currentCommands = new Stack<int>(BuilderInitSize);

        private readonly SplayTree<int, float> _results = new SplayTree<int, float>();

        #endregion

        #region Genesis

        // Needed because of unit testing
        public SplayGeneratorTests()
            : this(null)
        { }

        public SplayGeneratorTests(string logFolderName)
            : base(logFolderName, LogFileName, ConsumerCount)
        {
            OnFinished += Finished;
            StartConsumers(ProcessDataHandler);
        }

        #endregion

        #region Processing

        private enum CommandState
        {
            Init,
            Starts,
            Inserts,
            Finds,
        }

        private void GenerateHandler(string data)
        {
            if (data == null)
                return;

            try
            {
                switch (data[0])
                {
                    case '#':
                        Debug.Assert(_state == CommandState.Init || _state == CommandState.Finds); // A race condition -- another DataReceivedHandler changed the state

                        // Replace the builder by a new instance
                        Stack<int> commands = _currentCommands;

                        if (_state != CommandState.Init)
                        {
                            Buffer.Add(commands);

                            _currentCommands = new Stack<int>(commands.Count + BuilderSizeIncrement);
                        }

                        // Start gathering a new batch
                        _state = CommandState.Starts;
                        break;

                    case 'I':
                        Debug.Assert(_state == CommandState.Inserts || _state == CommandState.Starts); // A race condition -- another DataReceivedHandler changed the state

                        if (_state == CommandState.Starts)
                            _state = CommandState.Inserts;
                        break;

                    case 'F':
                        Debug.Assert(_state == CommandState.Finds || _state == CommandState.Inserts); // A race condition -- another DataReceivedHandler changed the state

                        if (_state == CommandState.Inserts)
                            _state = CommandState.Finds;
                        break;
                }

                _currentCommands.Push(int.Parse(data.Substring(2)));
            }
            catch (Exception ex)
            {
                CancellationTokenSource.Cancel();
                Console.WriteLine("Error in parsing generator data:\n" + ex.Message);
            }
        }

        private void ProcessDataHandler(Stack<int> commands)
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
                Buffer.WaitingItemCount,
                sw.ElapsedMilliseconds * 0.001,
                insertCount,
                findCount,
                avgInsertDepth,
                avgFindDepth);
        }

        private void Finished()
        {
            if (_currentCommands != null)
            {
                Buffer.Add(_currentCommands);
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

        public Task RunGeneratorSequential()
        {
            var pars = string.Format("-s {0} -b", Seed);
            return Generator.RunGenerator(Path.Combine(ToolsPath, GeneratorName), pars, GenerateHandler);
        }

        public void RunSubset(int T)
        {
            var generatorTask = RunGeneratorSubset(T);
            Run("T_test_" + T, generatorTask);
        }

        private void RunSeq()
        {
            var generatorTask = RunGeneratorSequential();
            Run("Seq_test", generatorTask);
        }

        #endregion


#if LONG_RUNNING_TESTS
        [TestMethod]
#endif
        public void Run10T()
        {
            RunSubset(10);
        }

#if LONG_RUNNING_TESTS
        [TestMethod]
#endif
        public void Run100T()
        {
            RunSubset(100);
        }

#if LONG_RUNNING_TESTS
        [TestMethod]
#endif
        public void Run1000T()
        {
            RunSubset(1000);
        }

#if LONG_RUNNING_TESTS
        [TestMethod]
#endif
        public void Run10000T()
        {
            RunSubset(10000);
        }

#if LONG_RUNNING_TESTS
        [TestMethod]
#endif
        public void Run100000T()
        {
            RunSubset(100000);
        }

#if LONG_RUNNING_TESTS
        [TestMethod]
#endif
        public void Run1000000T()
        {
            RunSubset(1000000);
        }


#if LONG_RUNNING_TESTS
        [TestMethod]
#endif
        public void RunSequential()
        {
            RunSeq();
        }
    }
}
