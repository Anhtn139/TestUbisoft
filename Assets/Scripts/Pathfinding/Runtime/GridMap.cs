using System;
using UnityEngine;

namespace TestUbisoft.Pathfinding
{
    public sealed class GridMap : IGridMap
    {
        private readonly GridNode[,] nodes;

        public GridMap(Vector3 origin, float cellSize, bool[,] walkableCells)
        {
            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be greater than zero.");
            }

            walkableCells = walkableCells ?? throw new ArgumentNullException(nameof(walkableCells));

            Width = walkableCells.GetLength(0);
            Depth = walkableCells.GetLength(1);

            if (Width <= 0 || Depth <= 0)
            {
                throw new ArgumentException("Grid must contain at least one cell.", nameof(walkableCells));
            }

            Origin = origin;
            CellSize = cellSize;
            nodes = new GridNode[Width, Depth];

            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    var coordinates = new Vector2Int(x, z);
                    nodes[x, z] = new GridNode(coordinates, GridToWorld(coordinates), walkableCells[x, z]);
                }
            }

            WorldBounds = new Bounds(
                Origin + new Vector3(Width * CellSize * 0.5f, 0f, Depth * CellSize * 0.5f),
                new Vector3(Width * CellSize, CellSize, Depth * CellSize));
        }

        public int Width { get; }
        public int Depth { get; }
        public float CellSize { get; }
        public Vector3 Origin { get; }
        public Bounds WorldBounds { get; }

        public bool TryGetNode(Vector2Int coordinates, out GridNode node)
        {
            if (!Contains(coordinates))
            {
                node = default;
                return false;
            }

            node = nodes[coordinates.x, coordinates.y];
            return true;
        }

        public bool TryWorldToGrid(Vector3 worldPosition, out Vector2Int coordinates)
        {
            int x = Mathf.FloorToInt((worldPosition.x - Origin.x) / CellSize);
            int z = Mathf.FloorToInt((worldPosition.z - Origin.z) / CellSize);
            coordinates = new Vector2Int(x, z);
            return Contains(coordinates);
        }

        public Vector3 GridToWorld(Vector2Int coordinates)
        {
            return Origin + new Vector3(
                (coordinates.x + 0.5f) * CellSize,
                0f,
                (coordinates.y + 0.5f) * CellSize);
        }

        public bool IsWalkable(Vector2Int coordinates)
        {
            return TryGetNode(coordinates, out GridNode node) && node.Walkable;
        }

        private bool Contains(Vector2Int coordinates)
        {
            return coordinates.x >= 0
                && coordinates.y >= 0
                && coordinates.x < Width
                && coordinates.y < Depth;
        }
    }
}
