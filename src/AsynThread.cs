using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncThreading
{
    public class AsyncThread
    {
        readonly SingleThreadedSynchronizationContext _context = new SingleThreadedSynchronizationContext();
        public void Start()
        {
            var thread = new Thread(() =>
            {
                _context.Run();
            });
            thread.Start();
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
                action();
                taskCompletionSource.SetResult();
            }, null);
            return taskCompletionSource.Task;
        }

    }
}
