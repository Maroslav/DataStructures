using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Utils.DataStructures
{
    public class AsyncBuffer<T> : IDisposable
    {
        #region Fields

        private readonly Queue<T> _queue;
        private readonly Queue<TaskCompletionSource<T>> _waitingTasks;

        public bool Disposed { get; private set; }

        public int WaitingItemCount { get { return _queue.Count; } }
        public int WaitingConsumerCount { get { return _waitingTasks.Count; } }

        #endregion

        #region Genesis

        public AsyncBuffer()
        {
            _queue = new Queue<T>();
            _waitingTasks = new Queue<TaskCompletionSource<T>>();
        }

        public AsyncBuffer(CancellationToken cancellationToken)
        {
            _queue = new Queue<T>();
            _waitingTasks = new Queue<TaskCompletionSource<T>>();

            cancellationToken.Register(Clear);
        }

        public void Dispose()
        {
            Disposed = true;
            Clear();
        }

        public void Clear()
        {
            lock (_queue)
            {
                foreach (TaskCompletionSource<T> taskCompletionSource in _waitingTasks)
                    taskCompletionSource.SetCanceled();

                _waitingTasks.Clear();
                _queue.Clear();
            }
        }

        #endregion

        #region Operations

        public void Add(T item)
        {
            if (Disposed)
                throw new ObjectDisposedException("AsyncBuffer");

            TaskCompletionSource<T> tcs = null;

            lock (_queue)
            {
                if (_waitingTasks.Count > 0)
                {
                    tcs = _waitingTasks.Dequeue();
                }
                else
                {
                    _queue.Enqueue(item);
                }
            }

            if (tcs != null)
            {
                tcs.TrySetResult(item);
            }
        }

        public Task<T> Get()
        {
            if (Disposed)
                throw new ObjectDisposedException("AsyncBuffer");

            lock (_queue)
            {
                if (_queue.Count > 0)
                    return Task.FromResult(_queue.Dequeue());

                var tcs = new TaskCompletionSource<T>();
                _waitingTasks.Enqueue(tcs);
                return tcs.Task;
            }
        }

        /// <summary>
        /// Returns false if there were no waiting items to get.
        /// </summary>
        public bool TryGet(out Task<T> item)
        {
            if (Disposed)
                throw new ObjectDisposedException("AsyncBuffer");

            lock (_queue)
            {
                if (_queue.Count > 0)
                {
                    item = Task.FromResult(_queue.Dequeue());
                    return true;
                }

                var tcs = new TaskCompletionSource<T>();
                _waitingTasks.Enqueue(tcs);
                item = tcs.Task;
                return false;
            }
        }

        #endregion
    }
}
