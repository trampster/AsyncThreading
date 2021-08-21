using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace AsyncThreading.Tests
{
    public class AsyncThreadTests
    {
        AsyncThread _asyncThread;
        CancellationTokenSource _cancellationSource;
        Task _threadTask;

        [SetUp]
        public void Setup()
        {
            _asyncThread = new AsyncThread(32);
            _cancellationSource = new CancellationTokenSource();
            _threadTask = _asyncThread.Start(_cancellationSource.Token);
        }

        [TearDown]
        public async Task TearDown()
        {
            _cancellationSource.Cancel();


            if(!_threadTask.IsCanceled && !_threadTask.IsFaulted)
            {
                await _threadTask;
                return;
            }
            await Task.CompletedTask;
        }


        [Test]
        public async Task RunInThreadAsync_Called_IsRun()
        {
            // arrange
            bool wasRun = false;

            // act
            await _asyncThread.RunInThreadAsync(() => 
            {
                wasRun = true;
                return Task.CompletedTask;
            });

            // assert
            Assert.That(wasRun);
        }

        [Test]
        public void RunInThreadAsync_Exception_ThrownOnCallingThread()
        {
            // arrange
            // act
            // assert
            Assert.ThrowsAsync<Exception>(() => _asyncThread.RunInThreadAsync(() => throw new Exception("failed")));
        }

        [Test]
        public async Task RunInThread_Exception_CallUnhandledException()
        {
            // arrange
            Exception capturedException = null;

            // act
            _asyncThread.RunInThread(() => throw new Exception("failed"));

            // assert
            try
            {
                await _threadTask;
            }
            catch(Exception exception)
            {
                capturedException = exception;
            }
            Assert.That(capturedException.Message, Is.EqualTo("failed"));
        }

        [Test]
        public async Task RunInThread_Await_ScheduledInSameThread()
        {
            // arrange
            int? beforeAwaitId = null;
            int? afterAwaitId = null;

            // act
            await _asyncThread.RunInThreadAsync(async () => 
            {
                beforeAwaitId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                await Task.Delay(20);
                afterAwaitId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            });

            // assert
            Assert.That(beforeAwaitId.HasValue, Is.True);
            Assert.That(afterAwaitId.HasValue, Is.True);
            Assert.That(beforeAwaitId.Value, Is.EqualTo(afterAwaitId.Value));
        }

        [Test]
        public async Task StartInCurrentThread_RunInThreadAsync_IsRun()
        {
            // arrange
            bool wasRun = false;
            var asyncThread = new AsyncThread(32);

            CancellationTokenSource cancellationTokenSource = new();
            var thread = new Thread(_ => asyncThread.StartInCurrentThread(cancellationTokenSource.Token));
            thread.Start();

            // act
            await asyncThread.RunInThreadAsync(() => 
            {
                wasRun = true;
                return Task.CompletedTask;
            });

            // assert
            Assert.That(wasRun);

            cancellationTokenSource.Cancel();
            thread.Join();
        }
    }
}