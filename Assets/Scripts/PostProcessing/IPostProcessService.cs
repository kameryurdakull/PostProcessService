namespace Game.Core.PostProcessing
{
    public interface IPostProcessService
    {
        void Apply(in PostProcessRequest request);
        void Set(PostProcessEffect effect, float value, float duration = 0.2f);
        void Pulse(
            PostProcessEffect effect,
            float peakValue,
            float attackDuration = 0.08f,
            float returnDuration = 0.24f);
        void Reset(PostProcessEffect effect, float duration = 0.2f);
        void ResetAll(float duration = 0.2f);
        void BlendWeight(float weight, float duration = 0.2f);
        void PlayPreset(PostProcessPreset preset, float duration = 0.35f);
        void StopPreset(PostProcessPresetChannel channel, float duration = 0.35f);
        void ResetPresets(float duration = 0.35f);
    }
}
