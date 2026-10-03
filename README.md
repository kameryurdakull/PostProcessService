# URP Post-Processing Toolkit

A reusable, event-driven post-processing toolkit for Unity 6.3 and URP 17.3. It provides numeric effect tweens, stackable Volume presets, and VContainer integration through your project's separately installed EventBus.

![Demo with multiple effects selected](Docs/PostProcessDemoMixPreview.png)

## Install

Install [VContainer 1.19.0](https://github.com/hadashiA/VContainer) and [DOTween Free](https://dotween.demigiant.com/getstarted) first. Run DOTween's setup panel after importing it. URP is a declared package dependency. Install your EventBus separately and connect it using the package's `IPostProcessEventBus` adapter contract. UniTask is required only if your chosen EventBus uses it.

Then paste this into **Unity Package Manager > Install package from Git URL**:

```text
https://github.com/kameryurdakull/PostProcessService.git?path=/Packages/com.kameryurdakull.post-process-service#v2.0.0
```

See the [package README](Packages/com.kameryurdakull.post-process-service/README.md) for exact prerequisite URLs, VContainer setup, runtime API, event examples, preset mixing, and troubleshooting. A `.unitypackage` alternative is stored in [`Releases`](Releases); install through one method only.

## Demo

Open this repository as a Unity project and run [`Assets/Scenes/PostProcessDemo.unity`](Assets/Scenes/PostProcessDemo.unity). Select multiple buttons to preview a mix; click an active effect again to disable it. The demo uses ProBuilder, uGUI, and Input System. These are outside the distributable package.

The UPM package lives under [`Packages/com.kameryurdakull.post-process-service`](Packages/com.kameryurdakull.post-process-service). It contains the service, inspectors, and `IPostProcessEventBus` contract. The repository's `Assets/Scripts/EventBus` is a standalone bus used by the demo and is excluded from both the UPM package and the 2.0.0 `.unitypackage`. Versions pinned to `v1.0.0` retain the previous bundled EventBus.

## License

[MIT](LICENSE.md) for this repository's service and EventBus code. External dependencies retain their own licenses.
