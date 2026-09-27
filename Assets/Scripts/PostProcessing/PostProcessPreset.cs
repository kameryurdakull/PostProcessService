using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core.PostProcessing
{
    public enum PostProcessPresetChannel
    {
        Environment,
        Gameplay,
        Cinematic
    }

    [CreateAssetMenu(fileName = "PostProcessPreset", menuName = "Post Processing/Preset")]
    public sealed class PostProcessPreset : ScriptableObject
    {
        [SerializeField] private VolumeProfile _profile;
        [SerializeField] private PostProcessPresetChannel _channel = PostProcessPresetChannel.Environment;
        [SerializeField] private float _priorityOffset = 10f;

        public VolumeProfile Profile => _profile;
        public PostProcessPresetChannel Channel => _channel;
        public float PriorityOffset => _priorityOffset;
    }
}
