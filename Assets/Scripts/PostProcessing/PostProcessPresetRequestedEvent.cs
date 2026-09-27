namespace Game.Core.PostProcessing
{
    public enum PostProcessPresetOperation
    {
        Play,
        StopPreset,
        StopChannel,
        ResetAll
    }

    public readonly struct PostProcessPresetRequestedEvent
    {
        public PostProcessPresetOperation Operation { get; }
        public PostProcessPreset Preset { get; }
        public PostProcessPresetChannel Channel { get; }
        public float Duration { get; }

        public PostProcessPresetRequestedEvent(
            PostProcessPresetOperation operation,
            PostProcessPreset preset,
            PostProcessPresetChannel channel,
            float duration = 0.35f)
        {
            Operation = operation;
            Preset = preset;
            Channel = channel;
            Duration = duration;
        }
    }
}
