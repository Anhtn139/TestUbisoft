#if UNITY_SERVER || UNITY_EDITOR
namespace TestUbisoft.Server.AI
{
    public interface IEggTargetSelectionStrategy
    {
        bool TrySelectTarget(BotTargetSelectionContext context, out EggTargetSelection selection);
    }
}
#endif
