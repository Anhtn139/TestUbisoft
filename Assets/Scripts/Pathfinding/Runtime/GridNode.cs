using UnityEngine;

namespace TestUbisoft.Pathfinding
{
    public readonly struct GridNode
    {
        public GridNode(Vector2Int coordinates, Vector3 worldPosition, bool walkable)
        {
            Coordinates = coordinates;
            WorldPosition = worldPosition;
            Walkable = walkable;
        }

        public Vector2Int Coordinates { get; }
        public Vector3 WorldPosition { get; }
        public bool Walkable { get; }
    }
}
