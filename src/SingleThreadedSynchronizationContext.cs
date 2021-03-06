using System;
using System.Threading;

namespace AsyncThreading
{
    public class WorkItem
    {
        int _ready = 1;
        SendOrPostCallback _callback;
        object _state;

        public SendOrPostCallback Callback 
        {
            get => _callback;
            set
            {
                Interlocked.Exchange(ref _callback, value);
            }
        }

        public object State 
        {
            get => _state;
            set
            {
                Interlocked.Exchange(ref _state, value);
            }
        }

        public bool Ready
        {
            get => _ready == 0;
            set 
            {
                Interlocked.Exchange(ref _ready, value ? 0 : 1);
            }
        }
    }

    public class SingleThreadedSynchronizationContext : SynchronizationContext
    {
        readonly WorkItemQueue _workItemsQueue;

        public SingleThreadedSynchronizationContext(int queueSize)
        {
            _workItemsQueue = new WorkItemQueue(queueSize);
        }

        public void Run(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _workItemsQueue.Cancel());
            SynchronizationContext.SetSynchronizationContext(this);
            while(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    (var callback, var state) = _workItemsQueue.Dequeue();
                    if(callback == null)
                    {
                        Console.WriteLine($"callback {callback}");
                    }
                    callback(state);
                }
                catch(OperationCanceledException)
                {
                }
            }
        }

        /// <summary>
        /// Dispatches an synchronous message to a synchronization context.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="state"></param>
        public override void Send(SendOrPostCallback callback, object state)
        {
            throw new NotImplementedException("Send blocks and you shouldn't be blocking");
        }

        /// <summary>
        /// Dispatches an asynchronous message to a synchronization context.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="state"></param>
        public override void Post(SendOrPostCallback callback, object state)
        {
            _workItemsQueue.Enqueue(callback, state);
        }
    }
}
