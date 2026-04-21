using TestUbisoft.Prototype.Client;
using TestUbisoft.Prototype.Core;
using TestUbisoft.Prototype.Messaging;
using TestUbisoft.Prototype.Presentation;
using TestUbisoft.Prototype.Server;
using TestUbisoft.Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TestUbisoft.Prototype.Bootstrap
{
    /// <summary>
    /// Composition root for the prototype.
    /// This MonoBehaviour creates services and drives ticks; gameplay rules stay in plain C# classes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        private static readonly Color[] BotPathColors =
        {
            new Color(0.98f, 0.36f, 0.24f, 1f),
            new Color(0.2f, 0.78f, 0.98f, 1f),
            new Color(0.98f, 0.82f, 0.22f, 1f),
            new Color(0.35f, 0.9f, 0.42f, 1f),
            new Color(0.94f, 0.44f, 0.96f, 1f)
        };

        [Header("Simulation")]
        [SerializeField] private int simulationTickRate = 60;
        [SerializeField] private float playerMoveSpeed = 5f;
        [SerializeField] private float botMoveSpeed = 3.5f;
        [SerializeField] private Vector2 playerSpawn = Vector2.zero;
        [SerializeField] private int botCount = 3;
        [SerializeField] private int eggCount = 30;
        [SerializeField] private float matchDurationSeconds = 120f;
        [SerializeField] private Vector2 snapshotIntervalRange = new Vector2(0.05f, 0.1f);
        [SerializeField] private Vector2 clientToServerLatencyRange = new Vector2(0.005f, 0.02f);
        [SerializeField] private Vector2 serverToClientLatencyRange = new Vector2(0.015f, 0.04f);
        [SerializeField] private float snapshotInterpolationDelay = 0.1f;
        [SerializeField] private float eggCollectionRadius = 0.75f;
        [SerializeField] private int botPathCandidateLimit = 5;
        [SerializeField] private int maxBotPathSearchesPerTick = 8;
        [SerializeField] private int maxServerTicksPerFrame = 5;
        [SerializeField] private bool disableLegacyAuthorityComponents = true;
        [SerializeField] private string localClientId = "local-player";
        [SerializeField] private GridPathfindingComponent pathfindingGrid = null;

        [Header("Presentation")]
        [SerializeField] private UnityClientWorldView worldView = null;
        [Header("Debug")]
        [SerializeField] private bool drawBotPathGizmos = true;
        [SerializeField, Min(0.01f)] private float botPathGizmoHeight = 0.2f;
        [SerializeField, Min(0.01f)] private float botPathNodeRadius = 0.14f;

        private GameConfig _config;
        private IServerSimulator _serverSimulator;
        private IMessageTransport _transport;
        private IClientGame _clientGame;
        private float _tickAccumulator;
        private double _serverTime;

        private void Awake()
        {
            if (disableLegacyAuthorityComponents)
            {
                DisableLegacyAuthorityComponents();
            }

            if (worldView == null)
            {
                worldView = FindObjectOfType<UnityClientWorldView>();
            }

            if (worldView == null)
            {
                GameObject worldViewObject = new GameObject("ClientWorldView");
                worldView = worldViewObject.AddComponent<UnityClientWorldView>();
            }

            if (pathfindingGrid == null)
            {
                pathfindingGrid = FindObjectOfType<GridPathfindingComponent>();
            }

            // Initialization order is explicit so each layer receives only the boundary it needs.
            _config = new GameConfig(
                localClientId,
                simulationTickRate,
                playerMoveSpeed,
                playerSpawn.ToSimVector2(),
                botCount,
                eggCount,
                matchDurationSeconds,
                botMoveSpeed,
                snapshotIntervalRange.ToSimVector2(),
                clientToServerLatencyRange.ToSimVector2(),
                serverToClientLatencyRange.ToSimVector2(),
                snapshotInterpolationDelay,
                eggCollectionRadius,
                botPathCandidateLimit,
                maxBotPathSearchesPerTick,
                maxServerTicksPerFrame);

            _serverSimulator = new ServerSimulator(pathfindingGrid);
            _transport = new SimulatedMessageTransport();
            _clientGame = new ClientGame();
            _serverTime = Time.timeAsDouble;

            _transport.ConfigureLatency(
                _config.ClientToServerLatencyRange.X,
                _config.ClientToServerLatencyRange.Y,
                _config.ServerToClientLatencyRange.X,
                _config.ServerToClientLatencyRange.Y);

            _transport.AdvanceTime(_serverTime);
            _serverSimulator.Initialize(_config, _transport);
            _clientGame.Initialize(_config, _transport, worldView);
        }

        private void Update()
        {
            if (_clientGame == null || _serverSimulator == null)
            {
                return;
            }

            _clientGame.SetLocalMoveInput(ReadMoveInput());

            _tickAccumulator += Time.deltaTime;
            int ticksThisFrame = 0;
            while (_tickAccumulator >= _config.FixedDeltaSeconds && ticksThisFrame < _config.MaxServerTicksPerFrame)
            {
                _serverTime += _config.FixedDeltaSeconds;
                _transport.AdvanceTime(_serverTime);
                _serverSimulator.Tick(_config.FixedDeltaSeconds, _serverTime);
                _tickAccumulator -= _config.FixedDeltaSeconds;
                ticksThisFrame++;
            }

            if (ticksThisFrame >= _config.MaxServerTicksPerFrame && _tickAccumulator >= _config.FixedDeltaSeconds)
            {
                _tickAccumulator = _config.FixedDeltaSeconds;
            }

            double clientTime = Time.timeAsDouble;
            _transport.AdvanceTime(clientTime);
            _clientGame.Tick(Time.deltaTime, clientTime);
        }

        private void OnDestroy()
        {
            _clientGame?.Shutdown();
            _serverSimulator?.Shutdown();
            _transport?.Clear();

            _clientGame = null;
            _serverSimulator = null;
            _transport = null;
            _config = null;
        }

        private void OnDrawGizmos()
        {
            if (!drawBotPathGizmos || _serverSimulator == null)
            {
                return;
            }

            IReadOnlyList<BotDebugPathSnapshot> botPaths = _serverSimulator.GetBotDebugPaths();
            for (int i = 0; i < botPaths.Count; i++)
            {
                DrawBotPathGizmo(botPaths[i], i);
            }
        }

        private static SimVector2 ReadMoveInput()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            return SimVector2.ClampMagnitude(new SimVector2(horizontal, vertical), 1f);
        }

        private void DrawBotPathGizmo(BotDebugPathSnapshot botPath, int colorIndex)
        {
            IReadOnlyList<SimVector2> path = botPath.Path;
            if (path == null || path.Count == 0 || botPath.PathIndex >= path.Count)
            {
                return;
            }

            Color color = BotPathColors[colorIndex % BotPathColors.Length];
            Vector3 previous = botPath.Position.ToUnityPosition(botPathGizmoHeight);

            Gizmos.color = color;
            Gizmos.DrawSphere(previous, botPathNodeRadius * 0.8f);

            for (int i = botPath.PathIndex; i < path.Count; i++)
            {
                Vector3 current = path[i].ToUnityPosition(botPathGizmoHeight);
                Gizmos.DrawLine(previous, current);
                Gizmos.DrawSphere(current, i == botPath.PathIndex ? botPathNodeRadius : botPathNodeRadius * 0.72f);
                previous = current;
            }
        }

        private static void DisableLegacyAuthorityComponents()
        {
            MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                string typeName = behaviour.GetType().FullName;
                if (!IsLegacyAuthorityType(typeName))
                {
                    continue;
                }

                behaviour.enabled = false;
            }
        }

        private static bool IsLegacyAuthorityType(string typeName)
        {
            return string.Equals(typeName, "EggCollecting.MatchFlow.MatchFlowController", StringComparison.Ordinal)
                || string.Equals(typeName, "EggCollecting.MatchFlow.RandomMatchSpawner", StringComparison.Ordinal)
                || string.Equals(typeName, "TestUbisoft.Server.AI.ServerEggBotController", StringComparison.Ordinal)
                || string.Equals(typeName, "TestUbisoft.Server.AI.BotGridMover", StringComparison.Ordinal);
        }
    }
}
