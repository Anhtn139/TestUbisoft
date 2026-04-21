#if UNITY_SERVER || UNITY_EDITOR
using UnityEngine;

namespace TestUbisoft.Server.AI
{
    public readonly struct BotTargetSelectionContext
    {
        public BotTargetSelectionContext(
            Vector3 botPosition,
            IEggProvider eggProvider,
            IPathfinder pathfinder)
        {
            BotPosition = botPosition;
            EggProvider = eggProvider;
            Pathfinder = pathfinder;
        }

        public Vector3 BotPosition { get; }

        public IEggProvider EggProvider { get; }

        public IPathfinder Pathfinder { get; }
    }
}
#endif
