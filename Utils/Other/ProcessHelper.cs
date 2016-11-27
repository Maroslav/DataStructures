using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Utils.Other
{
    [Flags]
    public enum WaitResult
    {
        Ok = 1,
        Failed = 2,
        TimedOut = 4,
    }

    public class ProcessHelper
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
            {
                if (!_p.HasExited)
                    _p.Kill();
                _p.Dispose();
            }

            _p = null;
        }

        #endregion


        public void StartProcess(string process, string arguments, string workingDirectory, CancellationToken token)
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
                    CreateNoWindow = false,
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

            token.Register(Dispose);

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

        public async Task<WaitResult> Wait(int msTimeout)
        {
            return await Task.Factory.StartNew(() =>
            {
                WaitResult res = 0;

                if (!_p.WaitForExit(msTimeout))
                    res |= WaitResult.TimedOut;

                if (!_noError)
                    res |= WaitResult.Failed;

                if (res == 0)
                    return WaitResult.Ok;

                return res;
            }).ConfigureAwait(false);
        }

        public TimeSpan GetElapsedTime()
        {
            return _p.ExitTime - _p.StartTime;
        }
    }
}
