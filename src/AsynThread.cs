using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncThreading
{
    public class AsyncThread
    {
        readonly SingleThreadedSynchronizationContext _context;

        public AsyncThread(int queueSize)
        {
            _context = new SingleThreadedSynchronizationContext(queueSize);
        }

        /// <summary>
        /// Starts in a new thread.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task Start(CancellationToken cancellationToken)
        {
            TaskCompletionSource threadFinished = new ();
            var thread = new Thread(() =>
            {
                try
                {
                    _context.Run(cancellationToken);
                    threadFinished.SetResult();
                }
                catch(Exception exception)
                {
                    threadFinished.SetException(exception);
                }
            });
            thread.Start();
            return threadFinished.Task;
        }

        /// <summary>
        /// Starts a new Syncronization context in the current thread.
        /// This blocks until the cancellation token is cancelled
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void StartInCurrentThread(CancellationToken cancellationToken)
        {
            if(SynchronizationContext.Current != null)
            {
                throw new InvalidOperationException("The current thread already has a syncronization context");
            }
            _context.Run(cancellationToken);
        }

        public void RunInThread(Action action)
        {
            _context.Post(_ => 
            {
                action();
            }, null);
        }

        public Task RunInThreadAsync(Func<Task> action)
        {
            var taskCompletionSource = new TaskCompletionSource();
            _context.Post(async _ => 
            {
                try
                {
                    await action();
                    taskCompletionSource.SetResult();
                }
                catch(Exception exception)
                {
                    taskCompletionSource.SetException(exception);
                }
            }, null);
            return taskCompletionSource.Task;
        }
    }
}
