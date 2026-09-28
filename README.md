# Post Process Service

A reusable, event-driven post-processing service for Unity 6.3 and URP 17.3. It provides numeric effect tweens, stackable Volume presets, VContainer integration, and an included EventBus.

![Demo with multiple effects selected](Docs/PostProcessDemoMixPreview.png)

## Install

Install [VContainer 1.19.0](https://github.com/hadashiA/VContainer), [UniTask 2.5.11](https://github.com/Cysharp/UniTask), and [DOTween Free](https://dotween.demigiant.com/getstarted) first. Run DOTween's setup panel after importing it. URP is a declared package dependency.

Then paste this into **Unity Package Manager > Install package from Git URL**:

```text
https://github.com/kameryurdakull/PostProcessService.git?path=/Packages/com.kameryurdakull.post-process-service#v1.0.0
```

See the [package README](Packages/com.kameryurdakull.post-process-service/README.md) for exact prerequisite URLs, VContainer setup, runtime API, event examples, preset mixing, and troubleshooting. A `.unitypackage` alternative is stored in [`Releases`](Releases); install through one method only.

## Demo

Open this repository as a Unity project and run [`Assets/Scenes/PostProcessDemo.unity`](Assets/Scenes/PostProcessDemo.unity). Select multiple buttons to preview a mix; click an active effect again to disable it. The demo uses ProBuilder, uGUI, and Input System. These are outside the distributable package.

The UPM package lives under [`Packages/com.kameryurdakull.post-process-service`](Packages/com.kameryurdakull.post-process-service). Its runtime and inspector assemblies include the service and EventBus; DOTween, VContainer, and UniTask remain external dependencies.

## License

[MIT](LICENSE.md) for this repository's service and EventBus code. External dependencies retain their own licenses.
