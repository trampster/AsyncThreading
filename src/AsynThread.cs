using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncThreading
{
    public class AsyncThread
    {
        readonly SingleThreadedSynchronizationContext _context = new SingleThreadedSynchronizationContext();
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

        public void RunInThread(Action action)
        {
            _context.Post(_ => 
            {
                action();
            }, null);
        }

        public Task RunInThreadAsync(Action action)
        {
            var taskCompletionSource = new TaskCompletionSource();
            _context.Post(_ => 
            {
                try
                {
                    action();
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
