using System.Collections.Generic;
using DG.Tweening;
using EventSystem;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VContainer;

namespace Game.Core.PostProcessing
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Volume))]
    public sealed class PostProcessVolumeExtension : MonoBehaviour, IPostProcessService
    {
        private const string MissingVolumeMessage =
            "Post process extension requires a Volume component with a valid profile.";
        private const float ChannelPriorityStep = 10f;

        private sealed class EffectBinding
        {
            public VolumeComponent Owner { get; }
            public VolumeParameter<float> Parameter { get; }
            public float InitialValue { get; }
            public bool InitialOverrideState { get; }
            public bool InitialOwnerState { get; }

            public EffectBinding(VolumeComponent owner, VolumeParameter<float> parameter)
            {
                Owner = owner;
                Parameter = parameter;
                InitialValue = parameter.value;
                InitialOverrideState = parameter.overrideState;
                InitialOwnerState = owner.active;
            }
        }

        private sealed class ActivePreset
        {
            public PostProcessPreset Preset { get; }
            public Volume Volume { get; }
            public Tween Tween { get; set; }

            public ActivePreset(PostProcessPreset preset, Volume volume)
            {
                Preset = preset;
                Volume = volume;
            }
        }

        private readonly Dictionary<PostProcessEffect, EffectBinding> _bindings = new();
        private readonly Dictionary<PostProcessEffect, Tween> _activeTweens = new();
        private readonly HashSet<PostProcessEffect> _engagedEffects = new();
        private readonly HashSet<PostProcessEffect> _persistentEffects = new();
        private readonly Dictionary<PostProcessPreset, ActivePreset> _activePresets = new();
        private readonly List<ActivePreset> _presetVolumes = new();

        private IEventBus _eventBus;
        private Volume _volume;
        private Tween _weightTween;
        private float _initialWeight;
        private bool _isSubscribed;
        private int _activationSequence;

        private void Awake()
        {
            if (!TryGetComponent<Volume>(out var volume) || volume.profile == null)
            {
                Debug.LogError(MissingVolumeMessage, this);
                enabled = false;
                return;
            }

            _volume = volume;
            _initialWeight = volume.weight;
            BuildBindings(volume.profile);
        }

        [Inject]
        public void Construct(IEventBus eventBus)
        {
            _eventBus = eventBus;
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            KillAllTweens();
            RestoreAllImmediately();
            _weightTween?.Kill();
            if (_volume != null) _volume.weight = _initialWeight;
            DestroyAllPresetVolumes();
        }

        public void Apply(in PostProcessRequest request)
        {
            switch (request.Operation)
            {
                case PostProcessOperation.Set:
                    Set(request.Effect, request.Value, request.Duration, request.Ease, request.IgnoreTimeScale);
                    break;
                case PostProcessOperation.Pulse:
                    Pulse(
                        request.Effect,
                        request.Value,
                        request.Duration,
                        request.ReturnDuration,
                        request.Ease,
                        request.IgnoreTimeScale);
                    break;
                case PostProcessOperation.Reset:
                    Reset(request.Effect, request.Duration, request.Ease, request.IgnoreTimeScale);
                    break;
            }
        }

        public void Set(PostProcessEffect effect, float value, float duration = 0.2f)
        {
            Set(effect, value, duration, Ease.OutCubic, true);
        }

        public void Pulse(
            PostProcessEffect effect,
            float peakValue,
            float attackDuration = 0.08f,
            float returnDuration = 0.24f)
        {
            Pulse(effect, peakValue, attackDuration, returnDuration, Ease.OutCubic, true);
        }

        public void Reset(PostProcessEffect effect, float duration = 0.2f)
        {
            Reset(effect, duration, Ease.OutCubic, true);
        }

        public void ResetAll(float duration = 0.2f)
        {
            var effects = new List<PostProcessEffect>(_engagedEffects);
            for (var effectIndex = 0; effectIndex < effects.Count; effectIndex++)
            {
                Reset(effects[effectIndex], duration);
            }

            ResetPresets(duration);
            BlendWeight(_initialWeight, duration);
        }

        public void BlendWeight(float weight, float duration = 0.2f)
        {
            if (_volume == null) return;

            _weightTween?.Kill();
            var targetWeight = Mathf.Clamp01(weight);
            if (duration <= 0f)
            {
                _volume.weight = targetWeight;
                return;
            }

            _weightTween = DOTween.To(() => _volume.weight, value => _volume.weight = value, targetWeight, duration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void PlayPreset(PostProcessPreset preset, float duration = 0.35f)
        {
            if (_volume == null || preset == null || preset.Profile == null) return;

            if (_activePresets.TryGetValue(preset, out var current))
            {
                TweenPreset(current, 1f, duration);
                return;
            }

            if (!preset.Stackable)
            {
                StopPreset(preset.Channel, duration);
            }

            var presetObject = new GameObject($"Post Process - {preset.name}");
            presetObject.transform.SetParent(transform, false);
            presetObject.layer = gameObject.layer;
            var presetVolume = presetObject.AddComponent<Volume>();
            presetVolume.isGlobal = true;
            presetVolume.priority = _volume.priority
                + preset.PriorityOffset
                + (int)preset.Channel * ChannelPriorityStep
                + ++_activationSequence * 0.001f;
            presetVolume.sharedProfile = preset.Profile;
            presetVolume.weight = 0f;

            var activePreset = new ActivePreset(preset, presetVolume);
            _presetVolumes.Add(activePreset);
            _activePresets.Add(preset, activePreset);
            TweenPreset(activePreset, 1f, duration);
        }

        public void StopPreset(PostProcessPreset preset, float duration = 0.35f)
        {
            if (preset == null || !_activePresets.Remove(preset, out var activePreset)) return;

            FadeOutPreset(activePreset, duration);
        }

        public void StopPreset(PostProcessPresetChannel channel, float duration = 0.35f)
        {
            var presets = new List<PostProcessPreset>();
            foreach (var pair in _activePresets)
            {
                if (pair.Key.Channel == channel) presets.Add(pair.Key);
            }

            for (var index = 0; index < presets.Count; index++) StopPreset(presets[index], duration);
        }

        public void ResetPresets(float duration = 0.35f)
        {
            var presets = new List<PostProcessPreset>(_activePresets.Keys);
            for (var index = 0; index < presets.Count; index++)
            {
                StopPreset(presets[index], duration);
            }
        }

        private void Set(
            PostProcessEffect effect,
            float value,
            float duration,
            Ease ease,
            bool ignoreTimeScale)
        {
            if (!TryGetBinding(effect, out var binding)) return;

            PrepareBinding(effect, binding);
            _persistentEffects.Add(effect);
            if (duration <= 0f)
            {
                binding.Parameter.value = value;
                return;
            }

            var tween = CreateValueTween(binding, value, duration, ease)
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            TrackTween(effect, tween);
        }

        private void Pulse(
            PostProcessEffect effect,
            float peakValue,
            float attackDuration,
            float returnDuration,
            Ease ease,
            bool ignoreTimeScale)
        {
            if (!TryGetBinding(effect, out var binding)) return;

            var returnValue = binding.Parameter.value;
            var returnOverrideState = binding.Parameter.overrideState;
            var wasPersistent = _persistentEffects.Contains(effect);
            PrepareBinding(effect, binding);
            var sequence = DOTween.Sequence();
            _ = sequence.Append(CreateValueTween(binding, peakValue, attackDuration, ease));
            _ = sequence.Append(CreateValueTween(
                binding,
                returnValue,
                returnDuration,
                ease));
            _ = sequence.OnComplete(() =>
            {
                binding.Parameter.overrideState = wasPersistent
                    ? returnOverrideState
                    : binding.InitialOverrideState;
                if (!wasPersistent) _engagedEffects.Remove(effect);
                RefreshOwnerState(binding.Owner);
            });
            _ = sequence.SetUpdate(ignoreTimeScale)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            TrackTween(effect, sequence);
        }

        private void Reset(
            PostProcessEffect effect,
            float duration,
            Ease ease,
            bool ignoreTimeScale)
        {
            if (!TryGetBinding(effect, out var binding)) return;
            if (!_engagedEffects.Contains(effect) && !_activeTweens.ContainsKey(effect)) return;

            KillTween(effect);
            _persistentEffects.Remove(effect);
            if (duration <= 0f)
            {
                RestoreBinding(effect, binding);
                return;
            }

            binding.Owner.active = true;
            binding.Parameter.overrideState = true;
            var tween = CreateValueTween(binding, binding.InitialValue, duration, ease)
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => RestoreBinding(effect, binding));
            TrackTween(effect, tween);
        }

        private void BuildBindings(VolumeProfile profile)
        {
            var bloom = GetOrAdd<Bloom>(profile);
            var vignette = GetOrAdd<Vignette>(profile);
            var chromaticAberration = GetOrAdd<ChromaticAberration>(profile);
            var lensDistortion = GetOrAdd<LensDistortion>(profile);
            var filmGrain = GetOrAdd<FilmGrain>(profile);
            var colorAdjustments = GetOrAdd<ColorAdjustments>(profile);

            AddBinding(PostProcessEffect.BloomIntensity, bloom, bloom.intensity);
            AddBinding(PostProcessEffect.VignetteIntensity, vignette, vignette.intensity);
            AddBinding(PostProcessEffect.ChromaticAberration, chromaticAberration, chromaticAberration.intensity);
            AddBinding(PostProcessEffect.LensDistortion, lensDistortion, lensDistortion.intensity);
            AddBinding(PostProcessEffect.FilmGrain, filmGrain, filmGrain.intensity);
            AddBinding(PostProcessEffect.PostExposure, colorAdjustments, colorAdjustments.postExposure);
            AddBinding(PostProcessEffect.Contrast, colorAdjustments, colorAdjustments.contrast);
            AddBinding(PostProcessEffect.Saturation, colorAdjustments, colorAdjustments.saturation);
            AddBinding(PostProcessEffect.HueShift, colorAdjustments, colorAdjustments.hueShift);
            AddBinding(PostProcessEffect.BloomThreshold, bloom, bloom.threshold);
            AddBinding(PostProcessEffect.BloomScatter, bloom, bloom.scatter);
            AddBinding(PostProcessEffect.VignetteSmoothness, vignette, vignette.smoothness);

            var whiteBalance = GetOrAdd<WhiteBalance>(profile);
            var depthOfField = GetOrAdd<DepthOfField>(profile);
            var motionBlur = GetOrAdd<MotionBlur>(profile);
            var colorLookup = GetOrAdd<ColorLookup>(profile);

            AddBinding(PostProcessEffect.WhiteBalanceTemperature, whiteBalance, whiteBalance.temperature);
            AddBinding(PostProcessEffect.WhiteBalanceTint, whiteBalance, whiteBalance.tint);
            AddBinding(PostProcessEffect.DepthOfFieldFocusDistance, depthOfField, depthOfField.focusDistance);
            AddBinding(PostProcessEffect.DepthOfFieldAperture, depthOfField, depthOfField.aperture);
            AddBinding(PostProcessEffect.DepthOfFieldFocalLength, depthOfField, depthOfField.focalLength);
            AddBinding(PostProcessEffect.DepthOfFieldGaussianStart, depthOfField, depthOfField.gaussianStart);
            AddBinding(PostProcessEffect.DepthOfFieldGaussianEnd, depthOfField, depthOfField.gaussianEnd);
            AddBinding(PostProcessEffect.MotionBlurIntensity, motionBlur, motionBlur.intensity);
            AddBinding(PostProcessEffect.MotionBlurClamp, motionBlur, motionBlur.clamp);
            AddBinding(PostProcessEffect.ColorLookupContribution, colorLookup, colorLookup.contribution);
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var component)) return component;

            component = profile.Add<T>(overrides: false);
            component.active = false;
            return component;
        }

        private void AddBinding(
            PostProcessEffect effect,
            VolumeComponent owner,
            VolumeParameter<float> parameter)
        {
            _bindings.Add(effect, new EffectBinding(owner, parameter));
        }

        private bool TryGetBinding(PostProcessEffect effect, out EffectBinding binding)
        {
            return _bindings.TryGetValue(effect, out binding);
        }

        private void PrepareBinding(PostProcessEffect effect, EffectBinding binding)
        {
            KillTween(effect);
            _engagedEffects.Add(effect);
            binding.Owner.active = true;
            binding.Parameter.overrideState = true;
        }

        private Tween CreateValueTween(
            EffectBinding binding,
            float targetValue,
            float duration,
            Ease ease)
        {
            return DOTween
                .To(() => binding.Parameter.value, value => binding.Parameter.value = value, targetValue, duration)
                .SetEase(ease);
        }

        private void TrackTween(PostProcessEffect effect, Tween tween)
        {
            _activeTweens[effect] = tween;
            _ = tween.OnKill(() => RemoveTrackedTween(effect, tween));
        }

        private void KillTween(PostProcessEffect effect)
        {
            if (!_activeTweens.TryGetValue(effect, out var tween)) return;

            _activeTweens.Remove(effect);
            tween.Kill();
        }

        private void KillAllTweens()
        {
            var tweens = new List<Tween>(_activeTweens.Values);
            _activeTweens.Clear();

            for (var tweenIndex = 0; tweenIndex < tweens.Count; tweenIndex++)
            {
                tweens[tweenIndex].Kill();
            }
        }

        private void RemoveTrackedTween(PostProcessEffect effect, Tween tween)
        {
            if (_activeTweens.TryGetValue(effect, out var activeTween) && activeTween == tween)
            {
                _activeTweens.Remove(effect);
            }
        }

        private void RestoreAllImmediately()
        {
            _engagedEffects.Clear();
            _persistentEffects.Clear();
            foreach (var pair in _bindings)
            {
                pair.Value.Parameter.value = pair.Value.InitialValue;
                pair.Value.Parameter.overrideState = pair.Value.InitialOverrideState;
            }

            foreach (var binding in _bindings.Values) binding.Owner.active = binding.InitialOwnerState;
        }

        private void RestoreBinding(PostProcessEffect effect, EffectBinding binding)
        {
            binding.Parameter.value = binding.InitialValue;
            binding.Parameter.overrideState = binding.InitialOverrideState;
            _engagedEffects.Remove(effect);
            _persistentEffects.Remove(effect);
            RefreshOwnerState(binding.Owner);
        }

        private void TweenPreset(ActivePreset preset, float targetWeight, float duration)
        {
            preset.Tween?.Kill();
            if (duration <= 0f)
            {
                preset.Volume.weight = targetWeight;
                return;
            }

            preset.Tween = DOTween.To(
                    () => preset.Volume.weight,
                    value => preset.Volume.weight = value,
                    targetWeight,
                    duration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void FadeOutPreset(ActivePreset preset, float duration)
        {
            TweenPreset(preset, 0f, duration);
            if (duration <= 0f)
            {
                DestroyPresetVolume(preset);
                return;
            }

            _ = preset.Tween.OnComplete(() => DestroyPresetVolume(preset));
        }

        private void DestroyPresetVolume(ActivePreset preset)
        {
            preset.Tween?.Kill();
            _presetVolumes.Remove(preset);
            if (preset.Volume != null) Destroy(preset.Volume.gameObject);
        }

        private void DestroyAllPresetVolumes()
        {
            _activePresets.Clear();
            _activationSequence = 0;
            for (var index = _presetVolumes.Count - 1; index >= 0; index--)
            {
                var preset = _presetVolumes[index];
                preset.Tween?.Kill();
                if (preset.Volume != null) Destroy(preset.Volume.gameObject);
            }

            _presetVolumes.Clear();
        }

        private void RefreshOwnerState(VolumeComponent owner)
        {
            foreach (var effect in _engagedEffects)
            {
                if (_bindings[effect].Owner != owner) continue;

                owner.active = true;
                return;
            }

            foreach (var binding in _bindings.Values)
            {
                if (binding.Owner != owner) continue;

                owner.active = binding.InitialOwnerState;
                return;
            }
        }

        private void OnPostProcessRequested(PostProcessRequestedEvent eventData)
        {
            var request = eventData.Request;
            Apply(in request);
        }

        private void OnPresetRequested(PostProcessPresetRequestedEvent eventData)
        {
            switch (eventData.Operation)
            {
                case PostProcessPresetOperation.Play:
                    PlayPreset(eventData.Preset, eventData.Duration);
                    break;
                case PostProcessPresetOperation.StopPreset:
                    StopPreset(eventData.Preset, eventData.Duration);
                    break;
                case PostProcessPresetOperation.StopChannel:
                    StopPreset(eventData.Channel, eventData.Duration);
                    break;
                case PostProcessPresetOperation.ResetAll:
                    ResetAll(eventData.Duration);
                    break;
            }
        }

        private void Subscribe()
        {
            if (_isSubscribed || _eventBus == null || !isActiveAndEnabled) return;

            _eventBus.Subscribe<PostProcessRequestedEvent>(OnPostProcessRequested);
            _eventBus.Subscribe<PostProcessPresetRequestedEvent>(OnPresetRequested);
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _eventBus == null) return;

            _eventBus.Unsubscribe<PostProcessRequestedEvent>(OnPostProcessRequested);
            _eventBus.Unsubscribe<PostProcessPresetRequestedEvent>(OnPresetRequested);
            _isSubscribed = false;
        }
    }
}
