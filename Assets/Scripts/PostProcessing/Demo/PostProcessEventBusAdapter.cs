using System;
using EventSystem;

namespace Game.Core.PostProcessing.Demo
{
    public sealed class PostProcessEventBusAdapter : IPostProcessEventBus
    {
        private readonly IEventBus _eventBus;

        public PostProcessEventBusAdapter(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Subscribe<T>(Action<T> callback) => _eventBus.Subscribe(callback);
        public void Unsubscribe<T>(Action<T> callback) => _eventBus.Unsubscribe(callback);
        public void Publish<T>(T eventMessage) => _eventBus.Publish(eventMessage);
    }
}
