using System;
using UnityEngine;

namespace TestUbisoft.Pathfinding
{
    public sealed class GridMapScanner
    {
        public GridMap Scan(
            Bounds worldBounds,
            float cellSize,
            LayerMask obstacleLayer,
            float obstacleCheckHeight = 2f,
            QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
        {
            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be greater than zero.");
            }

            int width = Mathf.Max(1, Mathf.CeilToInt(worldBounds.size.x / cellSize));
            int depth = Mathf.Max(1, Mathf.CeilToInt(worldBounds.size.z / cellSize));
            var walkableCells = new bool[width, depth];
            Vector3 origin = new Vector3(worldBounds.min.x, worldBounds.center.y, worldBounds.min.z);
            float checkHeight = Mathf.Max(0.01f, obstacleCheckHeight);
            Vector3 halfExtents = new Vector3(cellSize * 0.45f, checkHeight * 0.5f, cellSize * 0.45f);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector3 cellCenter = origin + new Vector3((x + 0.5f) * cellSize, 0f, (z + 0.5f) * cellSize);
                    bool blocked = Physics.CheckBox(
                        cellCenter,
                        halfExtents,
                        Quaternion.identity,
                        obstacleLayer,
                        triggerInteraction);

                    walkableCells[x, z] = !blocked;
                }
            }

            return new GridMap(origin, cellSize, walkableCells);
        }
    }
}
