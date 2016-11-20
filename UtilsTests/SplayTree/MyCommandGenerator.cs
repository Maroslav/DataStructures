using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.DataStructures;
using Utils.Other;

namespace UtilsTests.SplayTree
{
    internal class MyCommandGenerator
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
        private const int BuilderInitSize = 8000;
        private const int BuilderSizeIncrement = 3000;
        private const int Seed = 82;

        private const string ToolsPath = @"Tools";
        private const string GeneratorName = "generator.exe";

        private string FolderPath
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        #endregion

        #region Fields

        private readonly AsyncBuffer<Stack<int>> _buffer;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private CommandState _state = CommandState.Init;
        private Stack<int> _currentCommands = new Stack<int>(BuilderInitSize);

        #endregion

        #region Genesis

        public MyCommandGenerator(AsyncBuffer<Stack<int>> buffer, CancellationTokenSource cts)
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
                        Stack<int> commands = _currentCommands;

                        if (_state != CommandState.Init)
                        {
                            _buffer.Add(commands);

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

                _currentCommands.Push(int.Parse(e.Data.Substring(2)));
            }
            catch (Exception ex)
            {
                _cancellationTokenSource.Cancel();
                Console.WriteLine("Error in parsing generator data:\n" + ex.Message);
            }
        }

        public Task RunGeneratorSubset(int t)
        {
            Debug.Assert(t > 0);

            var pars = string.Format("-s {0} -t {1}", Seed, t);
            return RunGenerator(pars);
        }

        public Task RunGeneratorSequential()
        {
            var pars = string.Format("-s {0} -b", Seed);
            return RunGenerator(pars);
        }

        private async Task RunGenerator(string pars)
        {
            using (var generatorProcess = new ProcessHelper(
                Console.WriteLine,
                DataReceivedHandler,
                (sender, args) => Thread.CurrentThread.Abort(),
                null))
            {
                generatorProcess.StartProcess(
                    Path.Combine(ToolsPath, GeneratorName),
                    pars,
                    FolderPath,
                    _cancellationTokenSource.Token);

                var result = await generatorProcess.Wait(TimeOut).ConfigureAwait(false);

                if (result != WaitResult.Ok)
                    _cancellationTokenSource.Cancel();

                if (_currentCommands != null)
                {
                    _buffer.Add(_currentCommands);
                    _currentCommands = null;
                }

                Assert.IsTrue(result.HasFlag(WaitResult.Ok), "Generator process error: " + result);
                Console.WriteLine("Generator finished. Running time: " + generatorProcess.GetElapsedTime());
            }
        }

        #endregion
    }
}
