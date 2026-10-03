using System;

namespace Game.Core.PostProcessing
{
    /// <summary>Connects the service to a host project's synchronous event bus.</summary>
    public interface IPostProcessEventBus
    {
        void Subscribe<T>(Action<T> callback);
        void Unsubscribe<T>(Action<T> callback);
        void Publish<T>(T eventMessage);
    }
}
