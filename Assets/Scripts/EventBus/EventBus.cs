using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using VContainer;

namespace EventSystem
{
    public interface IEventBus
    {
        void Subscribe<T>(Action<T> callback);
        void Unsubscribe<T>(Action<T> callback);
        UniTask PublishAsync<T>(T eventData);
        void Publish<T>(T eventMessage);
    }

    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        [Inject]
        public EventBus()
        {
        }

        public void Subscribe<T>(Action<T> callback)
        {
            if (!_subscribers.ContainsKey(typeof(T)))
                _subscribers[typeof(T)] = new List<Delegate>();
            
            _subscribers[typeof(T)].Add(callback);
        }

        public void Unsubscribe<T>(Action<T> callback)
        {
            if (_subscribers.ContainsKey(typeof(T)))
                _subscribers[typeof(T)].Remove(callback);
        }

        public async UniTask PublishAsync<T>(T eventData)
        {
            if (_subscribers.TryGetValue(typeof(T), out var callbacks))
            {
                foreach (var callback in callbacks.ToArray())
                {
                    (callback as Action<T>)?.Invoke(eventData);
                }
            }
            
            await UniTask.Yield();
        }
        
        public void Publish<T>(T eventMessage)
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var subscriber)) return;

            var listeners = new List<object>(subscriber); 
            foreach (var listenerObj in listeners)
            {
                var listener = listenerObj as Action<T>;
                listener?.Invoke(eventMessage);
            }
        }
    }
}