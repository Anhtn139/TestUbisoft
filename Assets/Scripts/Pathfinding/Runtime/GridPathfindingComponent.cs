using System.Collections.Generic;
using UnityEngine;

namespace TestUbisoft.Pathfinding
{
    [DisallowMultipleComponent]
    public sealed class GridPathfindingComponent : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private Vector3 worldCenter = Vector3.zero;
        [SerializeField] private Vector3 worldSize = new Vector3(20f, 2f, 20f);
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float obstacleCheckHeight = 2f;
        [SerializeField] private bool scanOnAwake = true;

        [Header("Path Search")]
        [SerializeField] private bool allowDiagonalMovement = true;
        [SerializeField] private bool preventCornerCutting = true;

        [Header("Debug Gizmos")]
        [SerializeField] private bool drawGrid = true;
        [SerializeField] private bool drawPath = false;
        [SerializeField] private Color walkableColor = new Color(0f, 0.8f, 0.25f, 0.25f);
        [SerializeField] private Color unwalkableColor = new Color(0.9f, 0.05f, 0.05f, 0.55f);
        [SerializeField] private Color pathColor = new Color(0.05f, 0.45f, 1f, 1f);

        private readonly GridMapScanner scanner = new GridMapScanner();
        private IReadOnlyList<Vector3> currentPath = new List<Vector3>();

        public GridMap Map { get; private set; }
        public IPathfinder Pathfinder { get; private set; }
        public Bounds ScanBounds => new Bounds(worldCenter, worldSize);
        public IReadOnlyList<Vector3> CurrentPath => currentPath;

        private void Awake()
        {
            if (scanOnAwake)
            {
                Scan();
            }
        }

        [ContextMenu("Scan Grid")]
        public void Scan()
        {
            Map = scanner.Scan(ScanBounds, cellSize, obstacleLayer, obstacleCheckHeight);
            Pathfinder = new AStarPathfinder(Map, allowDiagonalMovement, preventCornerCutting);
            currentPath = new List<Vector3>();
        }

        public bool TryFindPath(Vector3 startWorldPosition, Vector3 targetWorldPosition, out IReadOnlyList<Vector3> path)
        {
            if (Pathfinder == null)
            {
                Scan();
            }

            bool found = Pathfinder.TryFindPath(startWorldPosition, targetWorldPosition, out path);
            currentPath = path;
            return found;
        }

        public IReadOnlyList<Vector3> FindPath(Vector3 startWorldPosition, Vector3 targetWorldPosition)
        {
            TryFindPath(startWorldPosition, targetWorldPosition, out IReadOnlyList<Vector3> path);
            return path;
        }

        public void SetDebugPath(IReadOnlyList<Vector3> path)
        {
            currentPath = path ?? new List<Vector3>();
        }

        private void OnDrawGizmos()
        {
            if (Map == null)
            {
                DrawScanBounds();
                return;
            }

            if (drawGrid)
            {
                DrawGridGizmos();
            }

            if (drawPath)
            {
                DrawPathGizmos();
            }
        }

        private void DrawScanBounds()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(worldCenter, worldSize);
        }

        private void DrawGridGizmos()
        {
            Vector3 cellSizeVector = new Vector3(Map.CellSize * 0.9f, 0.05f, Map.CellSize * 0.9f);

            for (int x = 0; x < Map.Width; x++)
            {
                for (int z = 0; z < Map.Depth; z++)
                {
                    var coordinates = new Vector2Int(x, z);

                    if (!Map.TryGetNode(coordinates, out GridNode node))
                    {
                        continue;
                    }

                    Gizmos.color = node.Walkable ? walkableColor : unwalkableColor;
                    Gizmos.DrawCube(node.WorldPosition, cellSizeVector);
                }
            }
        }

        private void DrawPathGizmos()
        {
            if (currentPath == null || currentPath.Count == 0)
            {
                return;
            }

            Gizmos.color = pathColor;
            float sphereRadius = Mathf.Max(0.05f, cellSize * 0.2f);

            for (int i = 0; i < currentPath.Count; i++)
            {
                Gizmos.DrawSphere(currentPath[i], sphereRadius);

                if (i > 0)
                {
                    Gizmos.DrawLine(currentPath[i - 1], currentPath[i]);
                }
            }
        }

        private void OnValidate()
        {
            cellSize = Mathf.Max(0.05f, cellSize);
            worldSize.x = Mathf.Max(cellSize, worldSize.x);
            worldSize.y = Mathf.Max(0.01f, worldSize.y);
            worldSize.z = Mathf.Max(cellSize, worldSize.z);
            obstacleCheckHeight = Mathf.Max(0.01f, obstacleCheckHeight);
        }
    }
}
