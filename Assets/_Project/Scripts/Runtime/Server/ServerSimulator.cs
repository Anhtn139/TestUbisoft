using System;
using System.Collections.Generic;
using TestUbisoft.Pathfinding;
using TestUbisoft.Prototype.Core;
using TestUbisoft.Prototype.Messaging;
using UnityEngine;

namespace TestUbisoft.Prototype.Server
{
    /// <summary>
    /// Prototype authoritative server simulation.
    /// Gameplay state changes live here rather than in MonoBehaviours or client-side view code.
    /// </summary>
    public sealed class ServerSimulator : IServerSimulator
    {
        private const float BotRepathInterval = 0.35f;
        private const float BotDeferredRepathInterval = 0.08f;
        private const float BotWaypointRadius = 0.12f;
        private const float BotStuckRepathDistance = 0.025f;

        private readonly Dictionary<string, ActorState> _actorsByClientId = new Dictionary<string, ActorState>();
        private readonly List<ActorState> _actors = new List<ActorState>(8);
        private readonly List<EggState> _eggs = new List<EggState>(64);
        private readonly List<EggCandidate> _eggCandidates = new List<EggCandidate>(64);
        private readonly List<SimVector2> _walkableSpawnPositions = new List<SimVector2>(128);
        private readonly List<EntitySnapshot> _snapshotBuffer = new List<EntitySnapshot>(128);
        private readonly List<ScoreSnapshot> _scoreBuffer = new List<ScoreSnapshot>(8);
        private readonly List<BotDebugPathSnapshot> _botDebugPathBuffer = new List<BotDebugPathSnapshot>(8);
        private readonly System.Random _random = new System.Random();

        private readonly GridPathfindingComponent _pathfindingGrid;

        private GameConfig _config;
        private IMessageTransport _transport;
        private int _tick;
        private int _nextEntityId = 1;
        private bool _isInitialized;
        private bool _matchStarted;
        private double _matchEndTime;
        private double _nextSnapshotTime;
        private int _remainingPathSearchesThisTick;

        public ServerSimulator(GridPathfindingComponent pathfindingGrid = null)
        {
            _pathfindingGrid = pathfindingGrid;
        }

        public void Initialize(GameConfig config, IMessageTransport transport)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));

            _actorsByClientId.Clear();
            _actors.Clear();
            _eggs.Clear();
            _walkableSpawnPositions.Clear();
            _snapshotBuffer.Clear();
            _scoreBuffer.Clear();
            _tick = 0;
            _nextEntityId = 1;
            _matchStarted = false;
            _matchEndTime = 0.0;
            _nextSnapshotTime = 0.0;
            _isInitialized = true;

            RefreshWalkableSpawnPositions();
            CreateMatchEntities();
        }

        public void Tick(float deltaSeconds, double serverTime)
        {
            if (!_isInitialized)
            {
                return;
            }

            if (!_matchStarted)
            {
                _matchStarted = true;
                _matchEndTime = serverTime + _config.MatchDurationSeconds;
                _nextSnapshotTime = serverTime;
            }

            while (_transport.TryDequeueForServer(out ClientInputMessage message))
            {
                ActorState player = EnsureLocalPlayer(message.ClientId);
                player.LastInputSequence = message.Sequence;
                player.MoveInput = SimVector2.ClampMagnitude(message.MoveInput, 1f);
            }

            bool publishedTerminalSnapshot = false;
            _remainingPathSearchesThisTick = _config.MaxBotPathSearchesPerTick;
            if (GetRemainingSeconds(serverTime) > 0f)
            {
                UpdateActors(deltaSeconds, serverTime);
                ResolveEggCollection();

                if (!HasAnyActiveEggs())
                {
                    _matchEndTime = serverTime;
                    PublishSnapshot(serverTime);
                    _nextSnapshotTime = serverTime + RandomRange(_config.SnapshotIntervalRange);
                    publishedTerminalSnapshot = true;
                }
            }

            _tick++;

            if (!publishedTerminalSnapshot && serverTime + 0.0001 >= _nextSnapshotTime)
            {
                PublishSnapshot(serverTime);
                _nextSnapshotTime = serverTime + RandomRange(_config.SnapshotIntervalRange);
            }
        }

        public void Shutdown()
        {
            _actorsByClientId.Clear();
            _actors.Clear();
            _eggs.Clear();
            _walkableSpawnPositions.Clear();
            _snapshotBuffer.Clear();
            _scoreBuffer.Clear();
            _botDebugPathBuffer.Clear();
            _transport = null;
            _config = null;
            _isInitialized = false;
            _matchStarted = false;
        }

        public IReadOnlyList<BotDebugPathSnapshot> GetBotDebugPaths()
        {
            _botDebugPathBuffer.Clear();

            for (int i = 0; i < _actors.Count; i++)
            {
                ActorState actor = _actors[i];
                if (actor.Kind != EntityKind.Bot)
                {
                    continue;
                }

                _botDebugPathBuffer.Add(new BotDebugPathSnapshot(
                    actor.EntityId,
                    actor.Position,
                    actor.PathIndex,
                    actor.Path));
            }

            return _botDebugPathBuffer;
        }

        private void CreateMatchEntities()
        {
            EnsureLocalPlayer(_config.LocalClientId);

            for (int i = 0; i < _config.BotCount; i++)
            {
                string botId = $"bot-{i + 1:00}";
                ActorState bot = new ActorState(
                    _nextEntityId++,
                    botId,
                    $"Bot {i + 1}",
                    EntityKind.Bot,
                    GetRandomSpawnPosition(i + 1));
                bot.NextRepathTime = RandomRange(new SimVector2(0f, BotRepathInterval));

                _actors.Add(bot);
                _actorsByClientId.Add(botId, bot);
            }

            for (int i = 0; i < _config.EggCount; i++)
            {
                _eggs.Add(new EggState(
                    _nextEntityId++,
                    $"egg-{i + 1:00}",
                    GetRandomSpawnPosition(i + 32),
                    i % 6));
            }
        }

        private ActorState EnsureLocalPlayer(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                clientId = _config.LocalClientId;
            }

            if (_actorsByClientId.TryGetValue(clientId, out ActorState player))
            {
                return player;
            }

            player = new ActorState(
                _nextEntityId++,
                clientId,
                "Player",
                EntityKind.Player,
                ClampToWalkableOrFallback(_config.PlayerSpawn));

            _actors.Add(player);
            _actorsByClientId.Add(clientId, player);
            return player;
        }

        private void UpdateActors(float deltaSeconds, double serverTime)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                ActorState actor = _actors[i];
                if (actor.Kind == EntityKind.Bot)
                {
                    UpdateBot(actor, deltaSeconds, serverTime);
                }
                else
                {
                    MoveActor(actor, actor.MoveInput, _config.PlayerMoveSpeed, deltaSeconds);
                }
            }
        }

        private void UpdateBot(ActorState bot, float deltaSeconds, double serverTime)
        {
            bool targetInvalid = bot.TargetEggEntityId == 0 || !IsEggActive(bot.TargetEggEntityId);
            bool pathMissing = bot.Path.Count == 0 || bot.PathIndex >= bot.Path.Count;

            if (targetInvalid)
            {
                if (serverTime >= bot.NextRepathTime || bot.TargetEggEntityId == 0)
                {
                    TrySelectBotTargetAndPath(bot, serverTime);
                }
            }
            else if (pathMissing && serverTime >= bot.NextRepathTime)
            {
                TryRefreshBotPath(bot, serverTime);
            }

            if (bot.Path.Count == 0 || bot.PathIndex >= bot.Path.Count)
            {
                return;
            }

            SimVector2 waypoint = bot.Path[bot.PathIndex];
            SimVector2 toWaypoint = waypoint - bot.Position;
            if (toWaypoint.Magnitude <= BotWaypointRadius)
            {
                bot.PathIndex++;
                if (bot.PathIndex >= bot.Path.Count)
                {
                    TryRefreshBotPath(bot, serverTime);
                    if (bot.Path.Count == 0 || bot.PathIndex >= bot.Path.Count)
                    {
                        return;
                    }

                    waypoint = bot.Path[bot.PathIndex];
                    toWaypoint = waypoint - bot.Position;
                }
            }

            SimVector2 previousPosition = bot.Position;
            MoveActor(bot, toWaypoint.Normalized, _config.BotMoveSpeed, deltaSeconds);

            if ((bot.Position - previousPosition).Magnitude < BotStuckRepathDistance * deltaSeconds)
            {
                bot.StuckSeconds += deltaSeconds;
                if (bot.StuckSeconds >= 0.25f)
                {
                    TryRefreshBotPath(bot, serverTime);
                    bot.StuckSeconds = 0f;
                }
            }
            else
            {
                bot.StuckSeconds = 0f;
            }
        }

        private void MoveActor(ActorState actor, SimVector2 direction, float speed, float deltaSeconds)
        {
            direction = SimVector2.ClampMagnitude(direction, 1f);
            if (direction.SqrMagnitude <= 0.0001f || speed <= 0f)
            {
                return;
            }

            SimVector2 step = direction * (speed * deltaSeconds);
            SimVector2 desired = actor.Position + step;

            if (!IsPositionWalkable(desired))
            {
                desired = FindAvoidancePosition(actor.Position, step);
            }

            if (IsPositionWalkable(desired))
            {
                actor.Position = desired;
                actor.YawDegrees = CalculateYawDegrees(direction);
            }
        }

        private SimVector2 FindAvoidancePosition(SimVector2 current, SimVector2 step)
        {
            SimVector2 direction = step.Normalized;
            SimVector2 side = new SimVector2(-direction.Y, direction.X);
            float distance = step.Magnitude;

            SimVector2 candidate = current + side * distance;
            if (IsPositionWalkable(candidate))
            {
                return candidate;
            }

            candidate = current - side * distance;
            if (IsPositionWalkable(candidate))
            {
                return candidate;
            }

            candidate = current + (direction + side).Normalized * distance;
            if (IsPositionWalkable(candidate))
            {
                return candidate;
            }

            candidate = current + (direction - side).Normalized * distance;
            return IsPositionWalkable(candidate) ? candidate : current;
        }

        private bool TrySelectBotTargetAndPath(ActorState bot, double serverTime)
        {
            if (_remainingPathSearchesThisTick <= 0)
            {
                bot.NextRepathTime = serverTime + BotDeferredRepathInterval;
                return false;
            }

            EggState bestEgg = null;
            IReadOnlyList<SimVector2> bestPath = null;
            float bestCost = float.PositiveInfinity;

            BuildNearestEggCandidates(bot.Position);
            int candidateLimit = Math.Min(_config.BotPathCandidateLimit, _eggCandidates.Count);
            for (int i = 0; i < candidateLimit; i++)
            {
                if (_remainingPathSearchesThisTick <= 0)
                {
                    break;
                }

                EggState egg = _eggCandidates[i].Egg;
                bot.ScratchPath.Clear();
                if (!TryFindPath(bot.Position, egg.Position, bot.ScratchPath))
                {
                    continue;
                }

                float cost = CalculatePathCost(bot.Position, bot.ScratchPath);
                if (cost >= bestCost)
                {
                    continue;
                }

                bestCost = cost;
                bestEgg = egg;
                bot.BestPath.Clear();
                bot.BestPath.AddRange(bot.ScratchPath);
                bestPath = bot.BestPath;
            }

            if (bestEgg == null && _eggCandidates.Count > 0)
            {
                bot.NextRepathTime = serverTime + BotDeferredRepathInterval;
                return false;
            }

            bot.TargetEggEntityId = bestEgg != null ? bestEgg.EntityId : 0;
            bot.Path.Clear();
            bot.PathIndex = 0;

            if (bestPath != null)
            {
                bot.Path.AddRange(bestPath);
            }

            bot.NextRepathTime = serverTime + BotRepathInterval + RandomRange(new SimVector2(0f, 0.15f));
            return bestEgg != null;
        }

        private bool TryRefreshBotPath(ActorState bot, double serverTime)
        {
            EggState currentTarget = FindEggByEntityId(bot.TargetEggEntityId);
            if (currentTarget != null && currentTarget.IsActive && TryAssignPathToEgg(bot, currentTarget, serverTime))
            {
                return true;
            }

            return TrySelectBotTargetAndPath(bot, serverTime);
        }

        private bool TryAssignPathToEgg(ActorState bot, EggState egg, double serverTime)
        {
            bot.ScratchPath.Clear();
            if (!TryFindPath(bot.Position, egg.Position, bot.ScratchPath))
            {
                bot.NextRepathTime = serverTime + BotDeferredRepathInterval;
                return false;
            }

            bot.TargetEggEntityId = egg.EntityId;
            bot.Path.Clear();
            bot.Path.AddRange(bot.ScratchPath);
            bot.PathIndex = 0;
            bot.NextRepathTime = serverTime + BotRepathInterval + RandomRange(new SimVector2(0f, 0.15f));
            return true;
        }

        private void BuildNearestEggCandidates(SimVector2 botPosition)
        {
            _eggCandidates.Clear();
            for (int i = 0; i < _eggs.Count; i++)
            {
                EggState egg = _eggs[i];
                if (!egg.IsActive)
                {
                    continue;
                }

                float distanceSqr = (egg.Position - botPosition).SqrMagnitude;
                _eggCandidates.Add(new EggCandidate(egg, distanceSqr));
            }

            _eggCandidates.Sort(CompareEggCandidates);
        }

        private bool TryFindPath(SimVector2 start, SimVector2 target, List<SimVector2> result)
        {
            result.Clear();
            _remainingPathSearchesThisTick--;

            if (_pathfindingGrid != null)
            {
                Vector3 startWorld = new Vector3(start.X, 0f, start.Y);
                Vector3 targetWorld = new Vector3(target.X, 0f, target.Y);
                if (!_pathfindingGrid.TryFindPath(startWorld, targetWorld, out IReadOnlyList<Vector3> path)
                    || path == null
                    || path.Count == 0)
                {
                    return false;
                }

                for (int i = 0; i < path.Count; i++)
                {
                    result.Add(new SimVector2(path[i].x, path[i].z));
                }

                return true;
            }

            result.Add(target);
            return true;
        }

        private void ResolveEggCollection()
        {
            float collectionRadiusSqr = _config.EggCollectionRadius * _config.EggCollectionRadius;

            for (int eggIndex = 0; eggIndex < _eggs.Count; eggIndex++)
            {
                EggState egg = _eggs[eggIndex];
                if (!egg.IsActive)
                {
                    continue;
                }

                ActorState collector = null;
                float bestDistanceSqr = float.PositiveInfinity;

                for (int actorIndex = 0; actorIndex < _actors.Count; actorIndex++)
                {
                    ActorState actor = _actors[actorIndex];
                    float distanceSqr = (actor.Position - egg.Position).SqrMagnitude;
                    if (distanceSqr <= collectionRadiusSqr && distanceSqr < bestDistanceSqr)
                    {
                        collector = actor;
                        bestDistanceSqr = distanceSqr;
                    }
                }

                if (collector == null)
                {
                    continue;
                }

                egg.IsActive = false;
                egg.CollectedByClientId = collector.ClientId;
                collector.Score++;

                for (int actorIndex = 0; actorIndex < _actors.Count; actorIndex++)
                {
                    ActorState actor = _actors[actorIndex];
                    if (actor.TargetEggEntityId == egg.EntityId)
                    {
                        actor.TargetEggEntityId = 0;
                        actor.Path.Clear();
                        actor.PathIndex = 0;
                    }
                }
            }
        }

        private void PublishSnapshot(double serverTime)
        {
            _snapshotBuffer.Clear();
            _scoreBuffer.Clear();

            for (int i = 0; i < _actors.Count; i++)
            {
                ActorState actor = _actors[i];
                _snapshotBuffer.Add(new EntitySnapshot(
                    actor.EntityId,
                    actor.ClientId,
                    actor.Kind,
                    actor.Position,
                    actor.YawDegrees));

                _scoreBuffer.Add(new ScoreSnapshot(actor.ClientId, actor.DisplayName, actor.Score));
            }

            for (int i = 0; i < _eggs.Count; i++)
            {
                EggState egg = _eggs[i];
                _snapshotBuffer.Add(new EntitySnapshot(
                    egg.EntityId,
                    egg.CollectedByClientId,
                    EntityKind.Egg,
                    egg.Position,
                    0f,
                    egg.IsActive,
                    egg.VisualIndex));
            }

            _transport.SendToClient(new ServerSnapshotMessage(new WorldSnapshot(
                _tick,
                serverTime,
                GetRemainingSeconds(serverTime),
                _snapshotBuffer,
                _scoreBuffer)));
        }

        private void RefreshWalkableSpawnPositions()
        {
            if (_pathfindingGrid == null)
            {
                return;
            }

            if (_pathfindingGrid.Map == null)
            {
                _pathfindingGrid.Scan();
            }

            GridMap map = _pathfindingGrid.Map;
            if (map == null)
            {
                return;
            }

            for (int x = 0; x < map.Width; x++)
            {
                for (int z = 0; z < map.Depth; z++)
                {
                    var coordinates = new Vector2Int(x, z);
                    if (!map.IsWalkable(coordinates))
                    {
                        continue;
                    }

                    Vector3 world = map.GridToWorld(coordinates);
                    _walkableSpawnPositions.Add(new SimVector2(world.x, world.z));
                }
            }
        }

        private SimVector2 GetRandomSpawnPosition(int salt)
        {
            if (_walkableSpawnPositions.Count > 0)
            {
                int index = _random.Next(0, _walkableSpawnPositions.Count);
                return _walkableSpawnPositions[(index + salt) % _walkableSpawnPositions.Count];
            }

            float x = RandomRange(new SimVector2(-8f, 8f));
            float y = RandomRange(new SimVector2(-8f, 8f));
            return new SimVector2(x, y);
        }

        private SimVector2 ClampToWalkableOrFallback(SimVector2 preferredPosition)
        {
            if (IsPositionWalkable(preferredPosition))
            {
                return preferredPosition;
            }

            return GetRandomSpawnPosition(0);
        }

        private bool IsPositionWalkable(SimVector2 position)
        {
            if (_pathfindingGrid == null || _pathfindingGrid.Map == null)
            {
                return true;
            }

            Vector3 world = new Vector3(position.X, 0f, position.Y);
            return _pathfindingGrid.Map.TryWorldToGrid(world, out Vector2Int coordinates)
                && _pathfindingGrid.Map.IsWalkable(coordinates);
        }

        private bool IsEggActive(int entityId)
        {
            for (int i = 0; i < _eggs.Count; i++)
            {
                if (_eggs[i].EntityId == entityId)
                {
                    return _eggs[i].IsActive;
                }
            }

            return false;
        }

        private EggState FindEggByEntityId(int entityId)
        {
            for (int i = 0; i < _eggs.Count; i++)
            {
                if (_eggs[i].EntityId == entityId)
                {
                    return _eggs[i];
                }
            }

            return null;
        }

        private bool HasAnyActiveEggs()
        {
            for (int i = 0; i < _eggs.Count; i++)
            {
                if (_eggs[i].IsActive)
                {
                    return true;
                }
            }

            return false;
        }

        private float GetRemainingSeconds(double serverTime)
        {
            if (!_matchStarted)
            {
                return _config != null ? _config.MatchDurationSeconds : 0f;
            }

            return Mathf.Max(0f, (float)(_matchEndTime - serverTime));
        }

        private float RandomRange(SimVector2 range)
        {
            if (range.Y <= range.X)
            {
                return range.X;
            }

            return range.X + (float)_random.NextDouble() * (range.Y - range.X);
        }

        private static float CalculatePathCost(SimVector2 start, List<SimVector2> path)
        {
            float cost = 0f;
            SimVector2 previous = start;
            for (int i = 0; i < path.Count; i++)
            {
                cost += (path[i] - previous).Magnitude;
                previous = path[i];
            }

            return cost;
        }

        private static float CalculateYawDegrees(SimVector2 moveInput)
        {
            return (float)(Math.Atan2(moveInput.X, moveInput.Y) * 180.0 / Math.PI);
        }

        private static int CompareEggCandidates(EggCandidate left, EggCandidate right)
        {
            return left.DistanceSqr.CompareTo(right.DistanceSqr);
        }

        private sealed class ActorState
        {
            public readonly int EntityId;
            public readonly string ClientId;
            public readonly string DisplayName;
            public readonly EntityKind Kind;
            public readonly List<SimVector2> Path = new List<SimVector2>(16);
            public readonly List<SimVector2> ScratchPath = new List<SimVector2>(16);
            public readonly List<SimVector2> BestPath = new List<SimVector2>(16);

            public int LastInputSequence;
            public int Score;
            public int TargetEggEntityId;
            public int PathIndex;
            public double NextRepathTime;
            public float StuckSeconds;
            public SimVector2 Position;
            public SimVector2 MoveInput;
            public float YawDegrees;

            public ActorState(int entityId, string clientId, string displayName, EntityKind kind, SimVector2 spawnPosition)
            {
                EntityId = entityId;
                ClientId = clientId;
                DisplayName = displayName;
                Kind = kind;
                Position = spawnPosition;
                MoveInput = SimVector2.Zero;
                YawDegrees = 0f;
            }
        }

        private readonly struct EggCandidate
        {
            public readonly EggState Egg;
            public readonly float DistanceSqr;

            public EggCandidate(EggState egg, float distanceSqr)
            {
                Egg = egg;
                DistanceSqr = distanceSqr;
            }
        }

        private sealed class EggState
        {
            public readonly int EntityId;
            public readonly int VisualIndex;
            public readonly SimVector2 Position;

            public string CollectedByClientId;
            public bool IsActive = true;

            public EggState(int entityId, string ownerId, SimVector2 position, int visualIndex)
            {
                EntityId = entityId;
                CollectedByClientId = ownerId;
                Position = position;
                VisualIndex = visualIndex;
            }
        }
    }
}
