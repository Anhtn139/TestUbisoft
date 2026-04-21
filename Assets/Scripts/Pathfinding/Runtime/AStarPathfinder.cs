using System;
using System.Collections.Generic;
using UnityEngine;

namespace TestUbisoft.Pathfinding
{
    public sealed class AStarPathfinder : IPathfinder
    {
        private const int StraightMoveCost = 10;
        private const int DiagonalMoveCost = 14;

        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        private static readonly Vector2Int[] DiagonalDirections =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(1, 0),
            new Vector2Int(1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, 1)
        };

        private readonly IGridMap gridMap;
        private readonly bool allowDiagonalMovement;
        private readonly bool preventCornerCutting;

        public AStarPathfinder(
            IGridMap gridMap,
            bool allowDiagonalMovement = true,
            bool preventCornerCutting = true)
        {
            this.gridMap = gridMap ?? throw new ArgumentNullException(nameof(gridMap));
            this.allowDiagonalMovement = allowDiagonalMovement;
            this.preventCornerCutting = preventCornerCutting;
        }

        public IReadOnlyList<Vector3> FindPath(Vector3 startWorldPosition, Vector3 targetWorldPosition)
        {
            return TryFindPath(startWorldPosition, targetWorldPosition, out IReadOnlyList<Vector3> path)
                ? path
                : Array.Empty<Vector3>();
        }

        public bool TryFindPath(
            Vector3 startWorldPosition,
            Vector3 targetWorldPosition,
            out IReadOnlyList<Vector3> path)
        {
            path = Array.Empty<Vector3>();

            if (!gridMap.TryWorldToGrid(startWorldPosition, out Vector2Int start)
                || !gridMap.TryWorldToGrid(targetWorldPosition, out Vector2Int target)
                || !gridMap.IsWalkable(start)
                || !gridMap.IsWalkable(target))
            {
                return false;
            }

            if (start == target)
            {
                path = new[] { gridMap.GridToWorld(start) };
                return true;
            }

            var open = new BinaryMinHeap();
            var closed = new HashSet<Vector2Int>();
            var records = new Dictionary<Vector2Int, PathRecord>();

            records[start] = new PathRecord(start, 0, GetHeuristicCost(start, target), start);
            open.Enqueue(start, records[start].FCost);

            while (open.Count > 0)
            {
                Vector2Int current = open.Dequeue();

                if (closed.Contains(current))
                {
                    continue;
                }

                if (current == target)
                {
                    path = BuildWorldPath(records, target);
                    return true;
                }

                closed.Add(current);

                foreach (Vector2Int neighbor in GetNeighbors(current))
                {
                    if (closed.Contains(neighbor) || !gridMap.IsWalkable(neighbor))
                    {
                        continue;
                    }

                    if (IsDiagonalMove(current, neighbor) && IsCornerBlocked(current, neighbor))
                    {
                        continue;
                    }

                    int tentativeGCost = records[current].GCost + GetMoveCost(current, neighbor);

                    if (!records.TryGetValue(neighbor, out PathRecord neighborRecord)
                        || tentativeGCost < neighborRecord.GCost)
                    {
                        int hCost = GetHeuristicCost(neighbor, target);
                        records[neighbor] = new PathRecord(neighbor, tentativeGCost, hCost, current);
                        open.Enqueue(neighbor, tentativeGCost + hCost);
                    }
                }
            }

            return false;
        }

        private IEnumerable<Vector2Int> GetNeighbors(Vector2Int coordinates)
        {
            Vector2Int[] directions = allowDiagonalMovement ? DiagonalDirections : CardinalDirections;

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int neighbor = coordinates + directions[i];

                if (gridMap.TryGetNode(neighbor, out _))
                {
                    yield return neighbor;
                }
            }
        }

        private bool IsCornerBlocked(Vector2Int current, Vector2Int neighbor)
        {
            if (!preventCornerCutting)
            {
                return false;
            }

            Vector2Int delta = neighbor - current;
            Vector2Int horizontal = current + new Vector2Int(delta.x, 0);
            Vector2Int vertical = current + new Vector2Int(0, delta.y);
            return !gridMap.IsWalkable(horizontal) || !gridMap.IsWalkable(vertical);
        }

        private int GetHeuristicCost(Vector2Int from, Vector2Int to)
        {
            int dx = Mathf.Abs(from.x - to.x);
            int dz = Mathf.Abs(from.y - to.y);

            if (!allowDiagonalMovement)
            {
                return StraightMoveCost * (dx + dz);
            }

            int diagonalSteps = Mathf.Min(dx, dz);
            int straightSteps = Mathf.Abs(dx - dz);
            return DiagonalMoveCost * diagonalSteps + StraightMoveCost * straightSteps;
        }

        private static bool IsDiagonalMove(Vector2Int from, Vector2Int to)
        {
            Vector2Int delta = to - from;
            return delta.x != 0 && delta.y != 0;
        }

        private static int GetMoveCost(Vector2Int from, Vector2Int to)
        {
            return IsDiagonalMove(from, to) ? DiagonalMoveCost : StraightMoveCost;
        }

        private IReadOnlyList<Vector3> BuildWorldPath(
            IReadOnlyDictionary<Vector2Int, PathRecord> records,
            Vector2Int target)
        {
            var coordinates = new List<Vector2Int>();
            Vector2Int current = target;

            while (true)
            {
                coordinates.Add(current);
                PathRecord record = records[current];

                if (record.Parent == current)
                {
                    break;
                }

                current = record.Parent;
            }

            coordinates.Reverse();

            var worldPath = new Vector3[coordinates.Count];
            for (int i = 0; i < coordinates.Count; i++)
            {
                worldPath[i] = gridMap.GridToWorld(coordinates[i]);
            }

            return worldPath;
        }

        private readonly struct PathRecord
        {
            public PathRecord(Vector2Int coordinates, int gCost, int hCost, Vector2Int parent)
            {
                Coordinates = coordinates;
                GCost = gCost;
                HCost = hCost;
                Parent = parent;
            }

            public Vector2Int Coordinates { get; }
            public int GCost { get; }
            public int HCost { get; }
            public int FCost => GCost + HCost;
            public Vector2Int Parent { get; }
        }

        private sealed class BinaryMinHeap
        {
            private readonly List<HeapItem> items = new List<HeapItem>();

            public int Count => items.Count;

            public void Enqueue(Vector2Int coordinates, int priority)
            {
                items.Add(new HeapItem(coordinates, priority));
                BubbleUp(items.Count - 1);
            }

            public Vector2Int Dequeue()
            {
                HeapItem root = items[0];
                int lastIndex = items.Count - 1;
                items[0] = items[lastIndex];
                items.RemoveAt(lastIndex);

                if (items.Count > 0)
                {
                    BubbleDown(0);
                }

                return root.Coordinates;
            }

            private void BubbleUp(int index)
            {
                while (index > 0)
                {
                    int parentIndex = (index - 1) / 2;

                    if (items[index].Priority >= items[parentIndex].Priority)
                    {
                        break;
                    }

                    Swap(index, parentIndex);
                    index = parentIndex;
                }
            }

            private void BubbleDown(int index)
            {
                while (true)
                {
                    int left = index * 2 + 1;
                    int right = left + 1;
                    int smallest = index;

                    if (left < items.Count && items[left].Priority < items[smallest].Priority)
                    {
                        smallest = left;
                    }

                    if (right < items.Count && items[right].Priority < items[smallest].Priority)
                    {
                        smallest = right;
                    }

                    if (smallest == index)
                    {
                        break;
                    }

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int a, int b)
            {
                HeapItem temp = items[a];
                items[a] = items[b];
                items[b] = temp;
            }

            private readonly struct HeapItem
            {
                public HeapItem(Vector2Int coordinates, int priority)
                {
                    Coordinates = coordinates;
                    Priority = priority;
                }

                public Vector2Int Coordinates { get; }
                public int Priority { get; }
            }
        }
    }
}
