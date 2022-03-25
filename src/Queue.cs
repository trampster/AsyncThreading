using System;
using System.Collections.Generic;
using System.Threading;

namespace AsyncThreading
{
    public class WorkItemQueue
    {
        readonly List<WorkItem> _items;
        ManualResetEvent _workItemAvailable = new ManualResetEvent(false);

        uint _enqueueIndex = 0; // index of last added item
        uint _dequeueIndex = 0; // index before the next item to be dequeued (increment first)

        public WorkItemQueue(int queueSize)
        {
            _items = new List<WorkItem>(queueSize);
            for(int index = 0; index < queueSize; index++)
            {
                _items.Add(new WorkItem());
            }
        }

        WorkItem GetItem(uint index)
        {
            lock(_items)
            {
                return _items[(int)(index % _items.Count)];
            }
        }

        void EnsureQueueSize()
        {
            lock(_items)
            {
                var enqueueIndex = (uint)(_enqueueIndex % _items.Count);
                var dequeueIndex = (uint)(_dequeueIndex % _items.Count);
                if(enqueueIndex < dequeueIndex)
                {
                    enqueueIndex += (uint)_items.Count;
                }

                uint count = enqueueIndex - dequeueIndex;
                if(count < _items.Count -1)
                {
                    return;
                }
                // double the size
                int oldSize = _items.Count;
                int newSize = _items.Count * 2;

                for(int index = 0; index < newSize/2; index++)
                {
                    _items.Add(new WorkItem());
                }
                uint oldEnqueueIndex = (uint)(_enqueueIndex % oldSize);
                _enqueueIndex = oldEnqueueIndex; //resets the index otherwise the mod on the new size will give a differnt index
                uint oldDequeueIndex = (uint)(_dequeueIndex % oldSize);
                _dequeueIndex = oldDequeueIndex; //resets the index otherwise the mod on the new size will give a differnt index

            }
        }

        public void Enqueue(SendOrPostCallback callback, object state)
        {
            EnsureQueueSize();
            uint index = Interlocked.Increment(ref _enqueueIndex);
            var item = GetItem(index);
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
            var item = GetItem(index);
            while(!item.Ready)
            {
                //spin until ready
                System.Threading.Thread.Yield();
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