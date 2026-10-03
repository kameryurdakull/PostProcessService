# Changelog

## 2.0.0

- Removed the bundled `EventSystem.EventBus` and `EventSystem.IEventBus` from the distribution.
- Added `Game.Core.PostProcessing.IPostProcessEventBus` as the host event-bus adapter contract.
- Changed `PostProcessVolumeExtension.Construct` to accept `IPostProcessEventBus`.
- Removed the package's UniTask assembly reference and activation constraint.
- Kept the standalone EventBus and its adapter in the repository's demo project only.
- Migration: register an adapter as `IPostProcessEventBus` in the same VContainer scope as the service. Install your own EventBus separately.

## 1.0.0

- URP post-process service with scalar controls, pulses, preset blending, and volume-weight blending.
- Stackable presets with explicit channel and per-preset stop operations.
- EventBus with allocation-free synchronous publishing after subscription setup.
- VContainer injection support and custom inspectors.
