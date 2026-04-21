using System.Collections.Generic;
using UnityEngine;

namespace TestUbisoft.Pathfinding
{
    public interface IPathfinder
    {
        IReadOnlyList<Vector3> FindPath(Vector3 startWorldPosition, Vector3 targetWorldPosition);
        bool TryFindPath(Vector3 startWorldPosition, Vector3 targetWorldPosition, out IReadOnlyList<Vector3> path);
    }
}
