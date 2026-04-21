using UnityEngine;

namespace TestUbisoft.Pathfinding
{
    public interface IGridMap
    {
        int Width { get; }
        int Depth { get; }
        float CellSize { get; }
        Vector3 Origin { get; }
        Bounds WorldBounds { get; }

        bool TryGetNode(Vector2Int coordinates, out GridNode node);
        bool TryWorldToGrid(Vector3 worldPosition, out Vector2Int coordinates);
        Vector3 GridToWorld(Vector2Int coordinates);
        bool IsWalkable(Vector2Int coordinates);
    }
}
