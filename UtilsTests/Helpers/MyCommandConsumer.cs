using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Utils.DataStructures;

namespace UtilsTests.Helpers
{
    public class MyCommandConsumer<TWorkItem>
    {
        private readonly AsyncBuffer<TWorkItem> _buffer;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Action<TWorkItem> _callback;

        public Task RequestCollectionTask;
        public bool ExitWhenNoItems;


        public MyCommandConsumer(AsyncBuffer<TWorkItem> buffer, CancellationTokenSource cts)
        {
            _buffer = buffer;
            _cancellationTokenSource = cts;
        }

        public void Start(Action<TWorkItem> callback)
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
                Task<TWorkItem> task;

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
                var result = task.Result; // Can throw if cancellation was requested (we want to end the thread anyway)
                _callback(result);
            }
        }

        #endregion
    }

}
