#if UNITY_SERVER || UNITY_EDITOR
namespace TestUbisoft.Server.AI
{
    public readonly struct EggTargetSelection
    {
        public EggTargetSelection(EggTarget target, PathResult path)
        {
            Target = target;
            Path = path;
        }

        public EggTarget Target { get; }

        public PathResult Path { get; }
    }
}
#endif
