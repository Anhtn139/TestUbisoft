#if UNITY_SERVER || UNITY_EDITOR
using TestUbisoft.Pathfinding;
using UnityEngine;

namespace TestUbisoft.Server.AI
{
    [DisallowMultipleComponent]
    public sealed class GridAStarPathfinderAdapter : MonoBehaviour, IPathfinder
    {
        [SerializeField] private GridPathfindingComponent gridPathfindingComponent;
        [SerializeField] private bool rescanBeforePathSearch;
        [SerializeField] private bool rescanBeforeBlockCheck;

        public bool TryFindPath(Vector3 start, Vector3 destination, out PathResult path)
        {
            path = default;

            GridPathfindingComponent gridPathfinder = ResolveGridPathfinder();
            if (gridPathfinder == null)
            {
                return false;
            }

            if (rescanBeforePathSearch)
            {
                gridPathfinder.Scan();
            }

            if (!gridPathfinder.TryFindPath(start, destination, out var waypoints)
                || waypoints == null
                || waypoints.Count == 0)
            {
                return false;
            }

            path = new PathResult(
                waypoints,
                PathResult.CalculateCost(waypoints),
                destination);

            return path.IsReachable;
        }

        public bool IsPathBlocked(PathResult path)
        {
            if (!path.IsReachable)
            {
                return true;
            }

            GridPathfindingComponent gridPathfinder = ResolveGridPathfinder();
            if (gridPathfinder == null)
            {
                return true;
            }

            if (rescanBeforeBlockCheck)
            {
                gridPathfinder.Scan();
            }

            GridMap map = gridPathfinder.Map;
            if (map == null)
            {
                return true;
            }

            for (var i = 0; i < path.WaypointCount; i++)
            {
                Vector3 waypoint = path.GetWaypointOrDestination(i);
                if (!map.TryWorldToGrid(waypoint, out var coordinates) || !map.IsWalkable(coordinates))
                {
                    return true;
                }
            }

            return false;
        }

        private GridPathfindingComponent ResolveGridPathfinder()
        {
            if (gridPathfindingComponent != null)
            {
                return gridPathfindingComponent;
            }

            gridPathfindingComponent = GetComponent<GridPathfindingComponent>();
            if (gridPathfindingComponent != null)
            {
                return gridPathfindingComponent;
            }

            gridPathfindingComponent = FindObjectOfType<GridPathfindingComponent>();
            return gridPathfindingComponent;
        }
    }
}
#endif
