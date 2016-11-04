using System;
using System.Diagnostics;

namespace Utils.Other
{
    class ProcessHelper
        : IDisposable
    {
        private Process _p;
        private bool _noError = true;

        private readonly Action<string> _verboseLog;
        private readonly DataReceivedEventHandler _processOutputDataReceived;
        private readonly DataReceivedEventHandler _processErrorDataReceived;
        private readonly EventHandler _processExited;


        public ProcessHelper(Action<string> verboseLog, DataReceivedEventHandler processOutputDataReceived, DataReceivedEventHandler processErrorDataReceived, EventHandler processExited)
        {
            _verboseLog = verboseLog;
            _processOutputDataReceived = processOutputDataReceived;
            _processErrorDataReceived = processErrorDataReceived;
            _processExited = processExited;
        }

        #region IDisposable overrides

        public void Dispose()
        {
            if (_p != null)
                _p.Dispose();

            _p = null;
        }

        #endregion


        public void StartProcess(string process, string arguments, string workingDirectory)
        {
            LogVerbose("*** StartProcess: " + process + " ***");

            _p = new Process
            {
                EnableRaisingEvents = true,
                StartInfo =
                {
                    FileName = process,
                    WorkingDirectory = workingDirectory,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            _p.OutputDataReceived += _processOutputDataReceived;
            _p.ErrorDataReceived += _processErrorDataReceived;
            _p.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    _noError = false;

                    LogVerbose(String.Format("*** {0} error: {1}", process, args.Data));
                }
            };
            _p.Exited += _processExited;
            _p.Exited += (sender, args) => LogVerbose(string.Format("*** {0} exited ***", process));

            try
            {
                bool started = _p.Start();
                Debug.Assert(started);
                _p.BeginOutputReadLine();
                _p.BeginErrorReadLine();

                LogVerbose("*** " + process + " started ***");
            }
            catch (Exception ex)
            {
                // It will not reach here because program will terminate first...
                LogVerbose("ERROR while starting process: " + process + "\n->" + ex.Message);
            }
        }

        void LogVerbose(string message)
        {
            if (_verboseLog != null)
                _verboseLog(message);
        }

        public bool Wait(int msTimeout)
        {
            _p.WaitForExit(msTimeout);
            return _noError;
        }
    }
}
