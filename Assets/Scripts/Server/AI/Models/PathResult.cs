#if UNITY_SERVER || UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace TestUbisoft.Server.AI
{
    public readonly struct PathResult
    {
        public PathResult(
            IReadOnlyList<Vector3> waypoints,
            float totalCost,
            Vector3 destination,
            bool isComplete = true,
            bool isBlocked = false)
        {
            Waypoints = waypoints;
            TotalCost = totalCost;
            Destination = destination;
            IsComplete = isComplete;
            IsBlocked = isBlocked;
        }

        public IReadOnlyList<Vector3> Waypoints { get; }

        public float TotalCost { get; }

        public Vector3 Destination { get; }

        public bool IsComplete { get; }

        public bool IsBlocked { get; }

        public bool IsReachable
        {
            get
            {
                return IsComplete
                    && !IsBlocked
                    && TotalCost >= 0f
                    && !float.IsNaN(TotalCost)
                    && !float.IsInfinity(TotalCost);
            }
        }

        public int WaypointCount
        {
            get { return Waypoints == null ? 0 : Waypoints.Count; }
        }

        public Vector3 GetWaypointOrDestination(int index)
        {
            if (Waypoints == null || Waypoints.Count == 0)
            {
                return Destination;
            }

            return Waypoints[Mathf.Clamp(index, 0, Waypoints.Count - 1)];
        }

        public static float CalculateCost(IReadOnlyList<Vector3> waypoints)
        {
            if (waypoints == null || waypoints.Count < 2)
            {
                return 0f;
            }

            var cost = 0f;
            for (var i = 1; i < waypoints.Count; i++)
            {
                cost += Vector3.Distance(waypoints[i - 1], waypoints[i]);
            }

            return cost;
        }
    }
}
#endif
