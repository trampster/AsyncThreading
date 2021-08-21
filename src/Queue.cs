using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncThreading
{
    public class WorkItemQueue
    {
        readonly WorkItem[] _items;
        ManualResetEvent _workItemAvailable = new ManualResetEvent(false);

        uint _enqueueIndex = 0; // index of last added item
        uint _dequeueIndex = 0; // index before the next item to be dequeued (increment first)

        public WorkItemQueue(int queueSize)
        {
            _items = new WorkItem[32];
            for(int index = 0; index < _items.Length; index++)
            {
                _items[index] = new WorkItem();
            }
        }

        public async void Enqueue(SendOrPostCallback callback, object state)
        {
            while(true)
            {
                var enqueueIndex = (uint)(_enqueueIndex % _items.Length);
                var dequeueIndex = (uint)(_dequeueIndex % _items.Length);
                if(enqueueIndex < dequeueIndex)
                {
                    enqueueIndex += (uint)_items.Length;
                }

                uint count = enqueueIndex - dequeueIndex;
                if(count < _items.Length -1)
                {
                    break;
                }
                await Task.Delay(100).ConfigureAwait(false); // the queue is full, this resceduals to a thread pool for a cooldown period
            }
            uint index = Interlocked.Increment(ref _enqueueIndex);
            var item = _items[index % _items.Length];
            item.Callback = callback;
            item.State = state;
            item.Ready = true;

            _workItemAvailable.Set();
        }

        enum State
        {
            NotCancelled,
            Cancelled
        }
        int _state = (int)State.NotCancelled;

        public void Cancel()
        {
            Interlocked.Exchange(ref _state, (int)State.Cancelled);
            _workItemAvailable.Set();
        }

        public (SendOrPostCallback, object) Dequeue()
        {
            //there is only one dequeuer
            if(_dequeueIndex == _enqueueIndex)
            {
                _workItemAvailable.Reset();
                //check if an item was enqueued just before calling reset
                if(_dequeueIndex == _enqueueIndex)
                {
                    _workItemAvailable.WaitOne();
                    if(_state == (int)State.Cancelled)
                    {
                        throw new OperationCanceledException();
                    }
                }
            }

            uint index = Interlocked.Increment(ref _dequeueIndex);
            var item = _items[index % _items.Length];
            while(!item.Ready)
            {
                //spin until ready
            }
            var callback = item.Callback;
            var state = item.State;
            item.Callback = null;
            item.State = null;
            item.Ready = false;

            return (callback, state);
        }
    }
}