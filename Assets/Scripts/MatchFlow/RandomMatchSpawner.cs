#if UNITY_SERVER || UNITY_EDITOR
using System;
using System.Collections.Generic;
using TestUbisoft.Pathfinding;
using TestUbisoft.Server.AI;
using UnityEngine;

namespace EggCollecting.MatchFlow
{
    [DefaultExecutionOrder(-200)]
    public sealed class RandomMatchSpawner : MonoBehaviour
    {
        private const int ActorLayer = 7;

        [Header("Grid")]
        [SerializeField] private GridPathfindingComponent gridPathfindingComponent;
        [SerializeField] private bool rescanGridBeforeSpawn = true;

        [Header("Prefabs")]
        [SerializeField] private GameObject eggPrefab;
        [SerializeField] private GameObject botPrefab;
        [SerializeField, Min(0)] private int eggCount = 8;
        [SerializeField, Min(0)] private int botCount = 1;
        [SerializeField] private Transform eggRoot;
        [SerializeField] private Transform botRoot;

        [Header("Server AI")]
        [SerializeField] private MonoBehaviour eggProviderBehaviour;
        [SerializeField] private MonoBehaviour pathfinderBehaviour;
        [SerializeField] private SceneEggProvider sceneEggProvider;
        [SerializeField] private bool refreshEggProviderAfterSpawn = true;

        [Header("Placement")]
        [SerializeField, Min(0f)] private float minimumSpawnDistance = 1.5f;
        [SerializeField] private float eggHeightOffset = 0.5f;
        [SerializeField] private float botHeightOffset = 1f;
        [SerializeField, Min(1)] private int maxPlacementAttempts = 256;

        [Header("Ground Snap")]
        [SerializeField] private bool snapToGround;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField, Min(0.01f)] private float groundRaycastHeight = 25f;

        [Header("Random")]
        [SerializeField] private bool useRandomSeed;
        [SerializeField] private int seed = 12345;

        [Header("Bot Defaults")]
        [SerializeField] private bool ensureBotMover = true;
        [SerializeField] private bool ensureBotIdentity = true;
        [SerializeField] private bool ensureBotRigidbody = true;

        private readonly List<Vector3> occupiedPositions = new List<Vector3>();

        private void Awake()
        {
            SpawnAll();
        }

        [ContextMenu("Spawn All")]
        public void SpawnAll()
        {
            occupiedPositions.Clear();

            GridMap map = ResolveMap();
            if (map == null)
            {
                Debug.LogWarning($"{nameof(RandomMatchSpawner)} needs a scanned {nameof(GridPathfindingComponent)}.", this);
                return;
            }

            System.Random random = useRandomSeed
                ? new System.Random(seed)
                : new System.Random(Environment.TickCount ^ GetInstanceID());

            SpawnEggs(map, random);
            SpawnBots(map, random);

            if (refreshEggProviderAfterSpawn)
            {
                RefreshEggProvider();
            }
        }

        private GridMap ResolveMap()
        {
            if (gridPathfindingComponent == null)
            {
                gridPathfindingComponent = FindObjectOfType<GridPathfindingComponent>();
            }

            if (gridPathfindingComponent == null)
            {
                return null;
            }

            if (rescanGridBeforeSpawn || gridPathfindingComponent.Map == null)
            {
                gridPathfindingComponent.Scan();
            }

            return gridPathfindingComponent.Map;
        }

        private void SpawnEggs(GridMap map, System.Random random)
        {
            if (eggPrefab == null)
            {
                return;
            }

            for (var i = 0; i < eggCount; i++)
            {
                if (!TryGetRandomPosition(map, random, eggHeightOffset, out Vector3 position))
                {
                    Debug.LogWarning($"Could not find a valid random egg spawn for index {i}.", this);
                    continue;
                }

                GameObject egg = Instantiate(eggPrefab, position, Quaternion.identity, eggRoot);
                egg.name = $"Egg_{i + 1:00}";
                ConfigureEgg(egg, i);
            }
        }

        private void SpawnBots(GridMap map, System.Random random)
        {
            if (botPrefab == null)
            {
                return;
            }

            MonoBehaviour eggProvider = eggProviderBehaviour != null ? eggProviderBehaviour : sceneEggProvider;
            MonoBehaviour pathfinder = pathfinderBehaviour != null ? pathfinderBehaviour : FindObjectOfType<GridAStarPathfinderAdapter>();

            for (var i = 0; i < botCount; i++)
            {
                if (!TryGetRandomPosition(map, random, botHeightOffset, out Vector3 position))
                {
                    Debug.LogWarning($"Could not find a valid random bot spawn for index {i}.", this);
                    continue;
                }

                GameObject bot = Instantiate(botPrefab, position, Quaternion.identity, botRoot);
                bot.name = $"Bot_{i + 1:00}";
                SetLayerRecursively(bot.transform, ActorLayer);
                ConfigureBot(bot, i, eggProvider, pathfinder);
            }
        }

        private void ConfigureEgg(GameObject egg, int index)
        {
            string eggId = $"egg-{index + 1:00}";

            EggPickup pickup = egg.GetComponent<EggPickup>();
            if (pickup != null)
            {
                pickup.ConfigureId(eggId);
                pickup.ServerResetForMatch();
            }

            ServerEggTarget target = egg.GetComponent<ServerEggTarget>();
            if (target != null)
            {
                target.Configure(eggId, true);
            }
        }

        private void ConfigureBot(
            GameObject bot,
            int index,
            MonoBehaviour eggProvider,
            MonoBehaviour pathfinder)
        {
            ServerMovementTargetOutput targetOutput = bot.GetComponent<ServerMovementTargetOutput>();
            if (targetOutput == null)
            {
                targetOutput = bot.AddComponent<ServerMovementTargetOutput>();
            }

            ServerEggBotController controller = bot.GetComponent<ServerEggBotController>();
            if (controller == null)
            {
                controller = bot.AddComponent<ServerEggBotController>();
            }

            controller.ConfigureDependencies(eggProvider, pathfinder, targetOutput);

            if (ensureBotRigidbody && bot.GetComponent<Rigidbody>() == null)
            {
                Rigidbody body = bot.AddComponent<Rigidbody>();
                body.useGravity = false;
                body.isKinematic = true;
            }

            if (ensureBotMover && bot.GetComponent<BotGridMover>() == null)
            {
                bot.AddComponent<BotGridMover>();
            }

            if (ensureBotIdentity)
            {
                PlayerIdentity identity = bot.GetComponent<PlayerIdentity>();
                if (identity == null)
                {
                    identity = bot.AddComponent<PlayerIdentity>();
                }

                identity.ConfigureIdentity($"bot-{index + 1:00}", $"Bot {index + 1}");
            }
        }

        private bool TryGetRandomPosition(
            GridMap map,
            System.Random random,
            float heightOffset,
            out Vector3 position)
        {
            float minimumDistanceSqr = minimumSpawnDistance * minimumSpawnDistance;

            for (var attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                var coordinates = new Vector2Int(
                    random.Next(0, map.Width),
                    random.Next(0, map.Depth));

                if (!map.IsWalkable(coordinates))
                {
                    continue;
                }

                Vector3 candidate = map.GridToWorld(coordinates) + Vector3.up * heightOffset;
                candidate = SnapToGround(candidate, heightOffset);

                if (IsTooCloseToExistingSpawn(candidate, minimumDistanceSqr))
                {
                    continue;
                }

                occupiedPositions.Add(candidate);
                position = candidate;
                return true;
            }

            position = default;
            return false;
        }

        private Vector3 SnapToGround(Vector3 position, float heightOffset)
        {
            if (!snapToGround || groundLayer.value == 0)
            {
                return position;
            }

            Vector3 rayOrigin = position + Vector3.up * groundRaycastHeight;
            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    groundRaycastHeight * 2f,
                    groundLayer,
                    QueryTriggerInteraction.Ignore))
            {
                position.y = hit.point.y + heightOffset;
            }

            return position;
        }

        private bool IsTooCloseToExistingSpawn(Vector3 candidate, float minimumDistanceSqr)
        {
            for (var i = 0; i < occupiedPositions.Count; i++)
            {
                Vector3 delta = candidate - occupiedPositions[i];
                delta.y = 0f;
                if (delta.sqrMagnitude < minimumDistanceSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshEggProvider()
        {
            SceneEggProvider provider = sceneEggProvider;
            if (provider == null)
            {
                provider = eggProviderBehaviour as SceneEggProvider;
            }

            if (provider == null)
            {
                provider = FindObjectOfType<SceneEggProvider>();
            }

            if (provider != null)
            {
                provider.RefreshEggsFromScene();
            }
        }

        private void OnValidate()
        {
            eggCount = Mathf.Max(0, eggCount);
            botCount = Mathf.Max(0, botCount);
            minimumSpawnDistance = Mathf.Max(0f, minimumSpawnDistance);
            maxPlacementAttempts = Mathf.Max(1, maxPlacementAttempts);
            groundRaycastHeight = Mathf.Max(0.01f, groundRaycastHeight);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }
    }
}
#endif
