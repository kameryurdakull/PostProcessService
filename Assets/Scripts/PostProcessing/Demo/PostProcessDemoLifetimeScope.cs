using EventSystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Core.PostProcessing.Demo
{
    public sealed class PostProcessDemoLifetimeScope : LifetimeScope
    {
        [SerializeField] private PostProcessVolumeExtension _service;
        [SerializeField] private PostProcessDemoController _controller;

        public void ConfigureReferences(PostProcessVolumeExtension service, PostProcessDemoController controller)
        {
            _service = service;
            _controller = controller;
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
            builder.Register<IPostProcessEventBus, PostProcessEventBusAdapter>(Lifetime.Singleton);
            builder.RegisterComponent(_service).As<IPostProcessService>();
            builder.RegisterComponent(_controller);
        }
    }
}
