using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils.DataStructures;

namespace UtilsTests.SplayTree
{
    internal class MyCommandConsumer
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

}
