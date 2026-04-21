#if UNITY_SERVER || UNITY_EDITOR
using UnityEngine;

namespace TestUbisoft.Server.AI
{
    public interface IPathfinder
    {
        bool TryFindPath(Vector3 start, Vector3 destination, out PathResult path);

        bool IsPathBlocked(PathResult path);
    }
}
#endif
