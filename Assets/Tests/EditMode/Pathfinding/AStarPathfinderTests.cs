using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TestUbisoft.Pathfinding.Tests
{
    public sealed class AStarPathfinderTests
    {
        [Test]
        public void TryFindPath_FindsShortestCardinalPathAroundBlockedCells()
        {
            bool[,] walkable = CreateWalkableGrid(5, 5);

            for (int z = 0; z < 4; z++)
            {
                walkable[2, z] = false;
            }

            var map = new GridMap(Vector3.zero, 1f, walkable);
            var pathfinder = new AStarPathfinder(map, allowDiagonalMovement: false);

            bool found = pathfinder.TryFindPath(
                map.GridToWorld(new Vector2Int(0, 0)),
                map.GridToWorld(new Vector2Int(4, 0)),
                out IReadOnlyList<Vector3> path);

            Assert.IsTrue(found);
            Assert.AreEqual(13, path.Count);
            AssertPathDoesNotUseBlockedCells(map, path);
        }

        [Test]
        public void TryFindPath_ReturnsFalseWhenTargetIsBlocked()
        {
            bool[,] walkable = CreateWalkableGrid(3, 3);
            walkable[2, 2] = false;

            var map = new GridMap(Vector3.zero, 1f, walkable);
            var pathfinder = new AStarPathfinder(map);

            bool found = pathfinder.TryFindPath(
                map.GridToWorld(new Vector2Int(0, 0)),
                map.GridToWorld(new Vector2Int(2, 2)),
                out IReadOnlyList<Vector3> path);

            Assert.IsFalse(found);
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void TryFindPath_PreventsDiagonalCornerCuttingByDefault()
        {
            bool[,] walkable = CreateWalkableGrid(3, 3);
            walkable[1, 0] = false;
            walkable[0, 1] = false;

            var map = new GridMap(Vector3.zero, 1f, walkable);
            var pathfinder = new AStarPathfinder(map, allowDiagonalMovement: true);

            bool found = pathfinder.TryFindPath(
                map.GridToWorld(new Vector2Int(0, 0)),
                map.GridToWorld(new Vector2Int(2, 2)),
                out _);

            Assert.IsFalse(found);
        }

        [Test]
        public void TryWorldToGrid_RejectsPositionsOutsideGridBounds()
        {
            var map = new GridMap(Vector3.zero, 1f, CreateWalkableGrid(2, 2));

            Assert.IsTrue(map.TryWorldToGrid(new Vector3(0.5f, 0f, 0.5f), out Vector2Int inside));
            Assert.AreEqual(new Vector2Int(0, 0), inside);
            Assert.IsFalse(map.TryWorldToGrid(new Vector3(2.1f, 0f, 0.5f), out _));
        }

        private static bool[,] CreateWalkableGrid(int width, int depth)
        {
            var walkable = new bool[width, depth];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    walkable[x, z] = true;
                }
            }

            return walkable;
        }

        private static void AssertPathDoesNotUseBlockedCells(GridMap map, IReadOnlyList<Vector3> path)
        {
            foreach (Vector3 worldPosition in path)
            {
                Assert.IsTrue(map.TryWorldToGrid(worldPosition, out Vector2Int coordinates));
                Assert.IsTrue(map.IsWalkable(coordinates), $"Path used blocked cell {coordinates}.");
            }
        }
    }
}
