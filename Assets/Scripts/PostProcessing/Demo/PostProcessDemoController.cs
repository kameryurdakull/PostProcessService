using System;
using System.Collections.Generic;
using DG.Tweening;
using EventSystem;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer;

namespace Game.Core.PostProcessing.Demo
{
    [Serializable]
    public sealed class PostProcessDemoEntry
    {
        [SerializeField] private PostProcessPreset _preset;
        [SerializeField] private Button _button;
        [SerializeField] private string _label;
        [SerializeField] private string _description;
        [SerializeField] private bool _animateCamera;

        public PostProcessPreset Preset => _preset;
        public Button Button => _button;
        public string Label => _label;
        public string Description => _description;
        public bool AnimateCamera => _animateCamera;

        public PostProcessDemoEntry(
            PostProcessPreset preset,
            Button button,
            string label,
            string description,
            bool animateCamera)
        {
            _preset = preset;
            _button = button;
            _label = label;
            _description = description;
            _animateCamera = animateCamera;
        }
    }

    public sealed class PostProcessDemoController : MonoBehaviour
    {
        private const float TransitionDuration = 0.45f;
        private const float CameraTravel = 1.15f;
        private const float CameraTravelDuration = 1.2f;

        [SerializeField] private PostProcessDemoEntry[] _entries;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Camera _sceneCamera;
        [SerializeField] private Color _normalButtonColor = new(0.10f, 0.15f, 0.22f, 0.95f);
        [SerializeField] private Color _selectedButtonColor = new(0.06f, 0.43f, 0.47f, 1f);

        private IEventBus _eventBus;
        private UnityAction[] _buttonActions;
        private Tween _cameraTween;
        private Vector3 _cameraHome;
        private readonly HashSet<int> _selectedIndices = new();
        private bool _cameraIsAnimating;
        private bool _previousRunInBackground;

        public void Configure(
            PostProcessDemoEntry[] entries,
            Button resetButton,
            Text statusText,
            Text descriptionText,
            Camera sceneCamera)
        {
            _entries = entries;
            _resetButton = resetButton;
            _statusText = statusText;
            _descriptionText = descriptionText;
            _sceneCamera = sceneCamera;
        }

        [Inject]
        public void Construct(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        private void Awake()
        {
            _previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            if (_sceneCamera != null) _cameraHome = _sceneCamera.transform.localPosition;
        }

        private void Start()
        {
            if (_eventBus == null || _entries == null || _resetButton == null)
            {
                Debug.LogError("Post process demo requires its event bus and configured UI references.", this);
                enabled = false;
                return;
            }

            _buttonActions = new UnityAction[_entries.Length];
            for (var index = 0; index < _entries.Length; index++)
            {
                var selectedIndex = index;
                _buttonActions[index] = () => Select(selectedIndex);
                _entries[index].Button.onClick.AddListener(_buttonActions[index]);
            }

            _resetButton.onClick.AddListener(ResetDemo);
            ShowStatus("BASELINE", "Select effects to build a live mix.");
            RefreshButtonColors();
        }

        private void OnDestroy()
        {
            if (_buttonActions != null)
            {
                for (var index = 0; index < _buttonActions.Length; index++)
                {
                    if (_entries[index].Button != null)
                        _entries[index].Button.onClick.RemoveListener(_buttonActions[index]);
                }
            }

            if (_resetButton != null) _resetButton.onClick.RemoveListener(ResetDemo);
            _cameraTween?.Kill();
            Application.runInBackground = _previousRunInBackground;
        }

        private void Select(int index)
        {
            if (_eventBus == null || index < 0 || index >= _entries.Length) return;

            var entry = _entries[index];
            if (_selectedIndices.Remove(index))
            {
                _eventBus.Publish(new PostProcessPresetRequestedEvent(
                    PostProcessPresetOperation.StopPreset,
                    entry.Preset,
                    entry.Preset.Channel,
                    TransitionDuration));
            }
            else
            {
                _selectedIndices.Add(index);
                _eventBus.Publish(new PostProcessPresetRequestedEvent(
                    PostProcessPresetOperation.Play,
                    entry.Preset,
                    entry.Preset.Channel,
                    TransitionDuration));
            }

            RefreshSelection();
        }

        private void ResetDemo()
        {
            if (_eventBus == null) return;

            _eventBus.Publish(new PostProcessPresetRequestedEvent(
                PostProcessPresetOperation.ResetAll,
                null,
                PostProcessPresetChannel.Environment,
                TransitionDuration));
            _selectedIndices.Clear();
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (_selectedIndices.Count == 0)
            {
                ShowStatus("BASELINE", "Select effects to build a live mix.");
            }
            else if (_selectedIndices.Count == 1)
            {
                foreach (var index in _selectedIndices)
                {
                    var entry = _entries[index];
                    ShowStatus(entry.Label, entry.Description);
                }
            }
            else
            {
                var labels = new List<string>();
                for (var index = 0; index < _entries.Length; index++)
                {
                    if (_selectedIndices.Contains(index)) labels.Add(_entries[index].Label);
                }

                var visibleCount = Mathf.Min(2, labels.Count);
                var summary = string.Join(" + ", labels.GetRange(0, visibleCount));
                if (labels.Count > visibleCount) summary += $" + {labels.Count - visibleCount} MORE";
                ShowStatus($"MIX / {_selectedIndices.Count} ACTIVE", summary);
            }

            RefreshButtonColors();
            var animateCamera = false;
            foreach (var index in _selectedIndices)
            {
                if (_entries[index].AnimateCamera) animateCamera = true;
            }

            SetCameraMotion(animateCamera);
        }

        private void SetCameraMotion(bool animate)
        {
            if (_cameraIsAnimating == animate) return;
            _cameraIsAnimating = animate;
            _cameraTween?.Kill();
            if (_sceneCamera == null) return;

            _sceneCamera.transform.localPosition = _cameraHome;
            if (!animate) return;

            _cameraTween = _sceneCamera.transform
                .DOLocalMoveX(_cameraHome.x + CameraTravel, CameraTravelDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        private void ShowStatus(string title, string description)
        {
            if (_statusText != null) _statusText.text = title;
            if (_descriptionText != null) _descriptionText.text = description;
        }

        private void RefreshButtonColors()
        {
            for (var index = 0; index < _entries.Length; index++)
            {
                var colors = _entries[index].Button.colors;
                colors.normalColor = _selectedIndices.Contains(index)
                    ? _selectedButtonColor
                    : _normalButtonColor;
                colors.selectedColor = colors.normalColor;
                _entries[index].Button.colors = colors;
            }
        }
    }
}
