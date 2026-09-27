using DG.Tweening;

namespace Game.Core.PostProcessing
{
    public enum PostProcessEffect
    {
        BloomIntensity,
        VignetteIntensity,
        ChromaticAberration,
        LensDistortion,
        FilmGrain,
        PostExposure,
        Contrast,
        Saturation,
        HueShift,
        BloomThreshold,
        BloomScatter,
        VignetteSmoothness,
        WhiteBalanceTemperature,
        WhiteBalanceTint,
        DepthOfFieldFocusDistance,
        DepthOfFieldAperture,
        DepthOfFieldFocalLength,
        DepthOfFieldGaussianStart,
        DepthOfFieldGaussianEnd,
        MotionBlurIntensity,
        MotionBlurClamp,
        ColorLookupContribution
    }

    public enum PostProcessOperation
    {
        Set,
        Pulse,
        Reset
    }

    public readonly struct PostProcessRequest
    {
        public PostProcessEffect Effect { get; }
        public PostProcessOperation Operation { get; }
        public float Value { get; }
        public float Duration { get; }
        public float ReturnDuration { get; }
        public Ease Ease { get; }
        public bool IgnoreTimeScale { get; }

        public PostProcessRequest(
            PostProcessEffect effect,
            PostProcessOperation operation,
            float value,
            float duration,
            float returnDuration = 0f,
            Ease ease = Ease.OutCubic,
            bool ignoreTimeScale = true)
        {
            Effect = effect;
            Operation = operation;
            Value = value;
            Duration = duration;
            ReturnDuration = returnDuration;
            Ease = ease;
            IgnoreTimeScale = ignoreTimeScale;
        }

        public static PostProcessRequest Set(
            PostProcessEffect effect,
            float value,
            float duration = 0.2f,
            Ease ease = Ease.OutCubic,
            bool ignoreTimeScale = true)
        {
            return new PostProcessRequest(
                effect,
                PostProcessOperation.Set,
                value,
                duration,
                ease: ease,
                ignoreTimeScale: ignoreTimeScale);
        }

        public static PostProcessRequest Pulse(
            PostProcessEffect effect,
            float peakValue,
            float attackDuration = 0.08f,
            float returnDuration = 0.24f,
            Ease ease = Ease.OutCubic,
            bool ignoreTimeScale = true)
        {
            return new PostProcessRequest(
                effect,
                PostProcessOperation.Pulse,
                peakValue,
                attackDuration,
                returnDuration,
                ease,
                ignoreTimeScale);
        }

        public static PostProcessRequest Reset(
            PostProcessEffect effect,
            float duration = 0.2f,
            Ease ease = Ease.OutCubic,
            bool ignoreTimeScale = true)
        {
            return new PostProcessRequest(
                effect,
                PostProcessOperation.Reset,
                0f,
                duration,
                ease: ease,
                ignoreTimeScale: ignoreTimeScale);
        }
    }
}
