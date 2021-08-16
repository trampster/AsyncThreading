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

    public class Subscriptions<TMessage> : ISubscriptions
    {
        readonly List<(SynchronizationContext, ISubscriber<TMessage>)?> _subscribers = new ();

        public void Publish(object message)
        {
            lock(_subscribers)
            {
                var typedMessage = (TMessage)message;
                for(int index = _subscribers.Count -1; index >= 0; index--)
                {
                    var pair = _subscribers[index];
                    if(!pair.HasValue)
                    {
                        _subscribers.RemoveAt(index);
                        continue;
                    }
                    (var context, var subscriber) = pair.Value;
                    
                    context.Post(_ => subscriber.OnMessageReceived(typedMessage), null);
                }
            }
        }

        public void Subscribe(SynchronizationContext context, object subscriber)
        {
            lock(_subscribers)
            {
                _subscribers.Add((context, (ISubscriber<TMessage>)subscriber));
            }
        }

        public void Unsubscribe(object subscriber)
        {
            lock(_subscribers)
            {
                for(int index = 0; index < _subscribers.Count; index++)
                {
                    var pair = _subscribers[index];
                    if(!pair.HasValue) continue;
                    (var context, var subscriberFromList) = pair.Value;
                    if(subscriber == subscriberFromList)
                    {
                        _subscribers[index] = null; //don't call remove because that could break the iteration if we are publishing
                        return;
                    }
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