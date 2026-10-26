namespace Character
{
    public interface IKFCProvider
    {
        public KungFuCircle KFC { get; }
        public StageController StageController { get; }
    }
}