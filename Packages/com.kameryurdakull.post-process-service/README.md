# Post Process Service

An event-driven URP post-processing service for Unity 6.3. It offers tweened numeric controls, profile-based presets, independent preset mixing, and a small EventBus. The package includes the runtime API and inspectors. The repository also contains a ProBuilder demo scene.

## Requirements

| Dependency | Tested version | Installation |
| --- | --- | --- |
| Unity | 6000.3.15f1 | Unity Hub |
| Universal Render Pipeline | 17.3.0 | Installed by this package through UPM |
| [VContainer](https://github.com/hadashiA/VContainer) | 1.19.0 | Git URL below |
| [UniTask](https://github.com/Cysharp/UniTask) | 2.5.11 | Git URL below |
| [DOTween Free](https://dotween.demigiant.com/getstarted) | 1.3.030 | Import into `Assets`, then run **Tools > Demigiant > DOTween Utility Panel > Setup DOTween** |

Install VContainer and UniTask in **Window > Package Manager > Install package from Git URL**:

```text
https://github.com/hadashiA/VContainer.git?path=/VContainer/Assets/VContainer#1.19.0
https://github.com/Cysharp/UniTask.git?path=/src/UniTask/Assets/Plugins/UniTask#2.5.11
```

Install and set up DOTween Free before adding this package. DOTween is distributed separately and is **not** bundled in this repository or the `.unitypackage`. [UPM cannot declare Git dependencies inside a package's `package.json`](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-git.html), so the host project installs them first. The service uses the standard DOTween DLL installation under `Assets/Plugins/Demigiant/DOTween`. The runtime and inspector assemblies activate when VContainer, UniTask, and DOTween's `DOTWEEN` scripting symbol are present; this allows the Git package to be added before setup without missing-reference compile errors.

## Install

Once the three external dependencies are present, add this Git URL in Package Manager:

```text
https://github.com/kameryurdakull/PostProcessService.git?path=/Packages/com.kameryurdakull.post-process-service#v1.0.0
```

For an unreleased checkout, use `#main` instead of `#v1.0.0`. The same runtime and inspector files are available as `Releases/PostProcessService-1.0.0.unitypackage`; import it through **Assets > Import Package > Custom Package**. Choose either UPM or `.unitypackage` for a project, not both.

## Scene setup

1. Use a URP renderer and a camera with **Post Processing** enabled. Include the Volume GameObject's layer in the camera's **Volume Mask**.
2. Create a global `Volume` with a `VolumeProfile`. Attach `PostProcessVolumeExtension` to the same GameObject.
3. Add a `LifetimeScope` and register the EventBus and service. Assign the service in the inspector.

```csharp
using EventSystem;
using Game.Core.PostProcessing;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private PostProcessVolumeExtension _postProcessService;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
        builder.RegisterComponent(_postProcessService).As<IPostProcessService>();
    }
}
```

`RegisterComponent` injects the EventBus into the service, and the service subscribes to the two request event types. Register your own event-bus implementation instead if your project already has one that implements `IEventBus`. Register and use only one bus instance per scope.

## Runtime API

Inject `IPostProcessService` into a VContainer-managed class, or keep a serialized reference to `PostProcessVolumeExtension`:

```csharp
using Game.Core.PostProcessing;
using VContainer;

public sealed class DamageFeedback
{
    private readonly IPostProcessService _postProcess;

    [Inject]
    public DamageFeedback(IPostProcessService postProcess)
    {
        _postProcess = postProcess;
    }

    public void Hit()
    {
        _postProcess.Pulse(PostProcessEffect.ChromaticAberration, 0.7f, 0.08f, 0.25f);
    }
}
```

`Set` tweens a numeric override to a target value; `Pulse` moves to a peak and restores the previous value; `Reset` restores one override; `ResetAll` also fades out presets and restores the base Volume weight. `BlendWeight` changes the base Volume's blend weight. Durations of `0` apply immediately.

Supported numeric effects include Bloom intensity/threshold/scatter, Vignette intensity/smoothness, Chromatic Aberration, Lens Distortion, Film Grain, exposure/contrast/saturation/hue, White Balance, Depth of Field, Motion Blur, and Color Lookup contribution. Use a `VolumeProfile` preset for non-numeric settings such as tonemapping mode, color filter, LUT texture, and Depth of Field mode.

## Event examples

The package includes `EventBus` and `IEventBus`. Request events are readonly structs. Synchronous `Publish` does not allocate per dispatch after subscription setup; `PublishAsync` invokes subscribers immediately and then yields through UniTask.

```csharp
using EventSystem;
using Game.Core.PostProcessing;
using VContainer;

public sealed class PostProcessEvents
{
    private readonly IEventBus _events;

    [Inject]
    public PostProcessEvents(IEventBus events)
    {
        _events = events;
    }

    public void IncreaseBloom()
    {
        var request = PostProcessRequest.Set(PostProcessEffect.BloomIntensity, 1.8f, 0.3f);
        _events.Publish(new PostProcessRequestedEvent(request));
    }

    public void ClearVignette()
    {
        var request = PostProcessRequest.Reset(PostProcessEffect.VignetteIntensity, 0.2f);
        _events.Publish(new PostProcessRequestedEvent(request));
    }
}
```

To use presets, create a `PostProcessPreset` from **Assets > Create > Post Processing > Preset**, assign its `VolumeProfile`, channel, and priority offset:

```csharp
using EventSystem;
using Game.Core.PostProcessing;
using UnityEngine;
using VContainer;

public sealed class WeatherLook : MonoBehaviour
{
    [SerializeField] private PostProcessPreset _rain;

    private IEventBus _events;

    [Inject]
    public void Construct(IEventBus events)
    {
        _events = events;
    }

    public void EnableRain()
    {
        _events.Publish(new PostProcessPresetRequestedEvent(
            PostProcessPresetOperation.Play, _rain, _rain.Channel, 0.4f));
    }

    public void DisableRain()
    {
        _events.Publish(new PostProcessPresetRequestedEvent(
            PostProcessPresetOperation.StopPreset, _rain, _rain.Channel, 0.4f));
    }
}
```

If this MonoBehaviour is not registered with VContainer, call `builder.RegisterComponentInHierarchy<WeatherLook>()` in your scope or inject it by another supported VContainer method. `StopChannel` fades every preset in a channel; `ResetAll` restores the service's complete state.

## Preset mixing

Check **Stackable** on presets that may run together. Non-stackable presets replace the currently active presets in their channel. Channels are ordered `Environment < Gameplay < Cinematic`; within the same channel and priority offset, the most recently activated preset wins when two profiles override the same property. The service creates one transient global Volume per active preset, fades its weight with DOTween, and destroys it after fade-out.

Mixing is a volume blend, not a mathematical addition of every parameter. When two profiles override the same parameter, URP resolves the result according to their weights and priorities. Keep expensive effects such as Depth of Field and Motion Blur selective on target platforms.

## Demo and troubleshooting

The repository's Unity project contains `Assets/Scenes/PostProcessDemo.unity` and `Assets/PostProcessingDemo/README.md`. Open the project with its listed dependencies to try 12 looks and a live multi-selection mix. The demo uses ProBuilder, uGUI, and Input System; they are not required by the distributable service package.

- **Missing `VContainer`, `UniTask`, or `DG.Tweening`:** install the external dependencies above before importing the service.
- **Package installed but service types are unavailable:** check that VContainer and UniTask appear in Package Manager, and that DOTween setup added `DOTWEEN` in **Project Settings > Player > Scripting Define Symbols** for the active build target.
- **Volume has no visible effect:** enable camera post-processing, check Volume Mask and weight, and ensure the profile has active overrides.
- **Assembly-name collision:** remove a previous copy of the service before switching between UPM and `.unitypackage`.

The EventBus and service are designed for Unity's main thread. Avoid creating a new tween every frame for a continuously changing value; use `Set` with duration `0` for direct updates, or issue a tween only when its target changes.

## License

The service and its EventBus are available under the [MIT License](LICENSE.md). VContainer, UniTask, DOTween, Unity packages, and ProBuilder retain their own licenses and are not redistributed here.
