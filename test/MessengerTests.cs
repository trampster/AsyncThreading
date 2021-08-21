using System.Threading.Tasks;
using NUnit.Framework;

namespace AsyncThreading.Tests
{
    public class MessengerTests
    {
        class MessageReceiver : ISubscriber<string>
        {
            public string ReceivedMessage {get;set;}

            public int ReceivingThread {get;set;}

            public Task OnMessageReceived(string message)
            {
                ReceivedMessage = message;
                ReceivingThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
                return Task.CompletedTask;
            }
        }

        [Test]
        public async Task Publish_OneSubscriber_HandlesMessageInSubscribersThread()
        {
            // arrage
            var messenger = new Messenger();
            AsyncThread asyncThread = new AsyncThread(32);

            var cancellationTokenSource = new System.Threading.CancellationTokenSource();
            var threadTask = asyncThread.Start(cancellationTokenSource.Token);

            var messageReceiver  = new MessageReceiver();
            int? subscribingThread = null;
            await asyncThread.RunInThreadAsync(() => 
            {
                messenger.Subscribe<string>(messageReceiver);
                subscribingThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
                return Task.CompletedTask;
            });

            // act
            messenger.Publish("hello thread");

            // this will wait for the thread to be finished with the message
            await asyncThread.RunInThreadAsync(() => Task.CompletedTask);

            // assert
            Assert.That(messageReceiver.ReceivedMessage, Is.EqualTo("hello thread"));
            Assert.That(messageReceiver.ReceivingThread, Is.EqualTo(subscribingThread));

            cancellationTokenSource.Cancel();
            await threadTask;
        }
    }

}