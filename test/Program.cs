using System;
using System.Threading.Tasks;

namespace AsyncThreading.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var messenger = new Messenger();

            var thread1 = new AsyncThread();
            thread1.Start();
            thread1.RunInThread(() => messenger.Subscribe(new MessageSubscriber()));

            var thread2 = new AsyncThread();
            thread2.Start();
            thread2.RunInThread(() => messenger.Subscribe(new MessageSubscriber()));


            System.Threading.Thread.Sleep(1000);
            
            messenger.Publish("Hello subscribers");


            while(true)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }

        
    }

    public class MessageSubscriber : ISubscriber<string>
    {
        public Task OnMessageReceived(string message)
        {
            Console.WriteLine($"Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}, {message}");
            return Task.CompletedTask;
        }
    }
}
