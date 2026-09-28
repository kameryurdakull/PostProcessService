namespace Game.Core.PostProcessing
{
    public readonly struct PostProcessRequestedEvent
    {
        public PostProcessRequest Request { get; }

        public PostProcessRequestedEvent(PostProcessRequest request)
        {
            Request = request;
        }
    }
}
