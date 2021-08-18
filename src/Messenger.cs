using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncThreading
{
    public interface ISubscriber<TMessage>
    {
        Task OnMessageReceived(TMessage message);
    }

    public interface ISubscriptions
    {
        void Publish(object message);

        void Subscribe(SynchronizationContext context, object subscriber);

        void Unsubscribe(object subscriber);
    }

    public class Subscription<TMessage> 
    {
        SynchronizationContext _context;
        ISubscriber<TMessage> _subscriber;
        bool _subscribed = false;

        public bool TrySubscribe(SynchronizationContext context, ISubscriber<TMessage> subscriber)
        {
            lock(this)
            {
                if(_subscribed)
                {
                    return false;
                }
                _context = context;
                _subscriber = subscriber;
                _subscribed = true;
                return true;
            }
        }

        public bool TryGet(out SynchronizationContext context, out ISubscriber<TMessage> subscriber)
        {
            lock(this)
            {
                context = _context;
                subscriber = _subscriber;
                return _subscribed;
            }
        }

        public bool TryUnsubscribe(ISubscriber<TMessage> subscriber)
        {
            lock(this)
            {
                if(subscriber != _subscriber)
                {
                    return false;
                }
                _context = null;
                _subscriber = null;
                _subscribed = false;
                return true;
            }
        }
    }

    public class Subscriptions<TMessage> : ISubscriptions
    {
        Subscription<TMessage>[] _subscribers = new Subscription<TMessage>[4];
        int _subscribeIndex = 0;

        public Subscriptions()
        {
            for(int index = 0; index < _subscribers.Length; index++)
            {
                _subscribers[index] = new Subscription<TMessage>();
            }
        }

        public void Publish(object message)
        {
            var typedMessage = (TMessage)message;
            for(int index = _subscribers.Length - 1; index >= 0; index--)
            {
                var subscription = _subscribers[index];
                if(!subscription.TryGet(out var context, out var subscriber))
                {
                    continue;
                }
                
                context.Post(_ => subscriber.OnMessageReceived(typedMessage), null);
            }
        }

        public void Subscribe(SynchronizationContext context, object subscriber)
        {
            var subscribers = _subscribers;
            for(int index = _subscribeIndex; index < subscribers.Length; index++)
            {
                int subIndex = index % subscribers.Length;
                var subscription = subscribers[subIndex];
                if(subscription.TrySubscribe(context, (ISubscriber<TMessage>)subscriber))
                {
                    return;
                }
            }
            //we did a full loop, there is no space, we need to expand.
            lock(_subscribers)
            {
                if(_subscribers.Length != subscribers.Length)
                {
                    //anther thread got there first
                    Subscribe(context, subscriber);
                }
                var newArray = new Subscription<TMessage>[_subscribers.Length * 2];
                for(int index = 0; index < _subscribers.Length; index++)
                {
                    newArray[index] = _subscribers[index];
                }
                for(int index = _subscribers.Length; index < newArray.Length; index++)
                {
                    newArray[index] = new Subscription<TMessage>();

                }
                Interlocked.Exchange(ref _subscribers, newArray);
                Subscribe(context, subscriber);
            }
        }

        public void Unsubscribe(object subscriber)
        {
            for(int index = 0; index < _subscribers.Length; index++)
            {
                var candidateSubscriber = _subscribers[index];
                if(candidateSubscriber.TryUnsubscribe((ISubscriber<TMessage>)subscriber))
                {
                    return;
                }
            }
        }
    }

    public class Messenger
    {
        readonly System.Collections.Concurrent.ConcurrentDictionary<Type, ISubscriptions> _subscriptions = new ();

        public void Subscribe<TMessage>(ISubscriber<TMessage> callback)
        {
            while(true)
            {
                if(_subscriptions.TryGetValue(typeof(TMessage), out var subscriptions))
                {
                    subscriptions.Subscribe(SynchronizationContext.Current, callback);
                    return;
                }
                _subscriptions.TryAdd(typeof(TMessage), new Subscriptions<TMessage>());
            }
        }

        public void Publish<TMessage>(TMessage message)
        {
            if(_subscriptions.TryGetValue(typeof(TMessage), out var subscriptions))
            {
                subscriptions.Publish(message);
            }
        }

        public void Unsubscribe<TMessage>(ISubscriber<TMessage> callback)
        {
            if(_subscriptions.TryGetValue(typeof(TMessage), out var subscriptions))
            {
                subscriptions.Unsubscribe(callback);
            }
        }
    }
}