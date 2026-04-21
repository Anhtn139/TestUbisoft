using System.Collections.Generic;
using TestUbisoft.Prototype.Core;

namespace TestUbisoft.Prototype.Server
{
    public readonly struct BotDebugPathSnapshot
    {
        public readonly int EntityId;
        public readonly SimVector2 Position;
        public readonly int PathIndex;
        public readonly IReadOnlyList<SimVector2> Path;

        public BotDebugPathSnapshot(int entityId, SimVector2 position, int pathIndex, IReadOnlyList<SimVector2> path)
        {
            EntityId = entityId;
            Position = position;
            PathIndex = pathIndex;
            Path = path;
        }
    }
}
