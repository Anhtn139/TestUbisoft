using System;

namespace TestUbisoft.Prototype.Core
{
    /// <summary>
    /// Runtime configuration shared by server simulation, transport, and client presentation.
    /// In production this can be loaded from a ScriptableObject, remote config, or matchmaking payload.
    /// </summary>
    public sealed class GameConfig
    {
        public string LocalClientId { get; }
        public int SimulationTickRate { get; }
        public float FixedDeltaSeconds => 1f / SimulationTickRate;
        public float PlayerMoveSpeed { get; }
        public float BotMoveSpeed { get; }
        public SimVector2 PlayerSpawn { get; }
        public int BotCount { get; }
        public int EggCount { get; }
        public float MatchDurationSeconds { get; }
        public SimVector2 SnapshotIntervalRange { get; }
        public SimVector2 ClientToServerLatencyRange { get; }
        public SimVector2 ServerToClientLatencyRange { get; }
        public float SnapshotInterpolationDelay { get; }
        public float EggCollectionRadius { get; }
        public float BotActorAvoidanceRadius { get; }
        public int BotPathCandidateLimit { get; }
        public int MaxBotPathSearchesPerTick { get; }
        public int MaxServerTicksPerFrame { get; }

        public GameConfig(
            string localClientId,
            int simulationTickRate,
            float playerMoveSpeed,
            SimVector2 playerSpawn,
            int botCount = 3,
            int eggCount = 30,
            float matchDurationSeconds = 120f,
            float botMoveSpeed = 3.5f,
            SimVector2 snapshotIntervalRange = default,
            SimVector2 clientToServerLatencyRange = default,
            SimVector2 serverToClientLatencyRange = default,
            float snapshotInterpolationDelay = 0.1f,
            float eggCollectionRadius = 0.75f,
            float botActorAvoidanceRadius = 0.65f,
            int botPathCandidateLimit = 5,
            int maxBotPathSearchesPerTick = 8,
            int maxServerTicksPerFrame = 5)
        {
            if (string.IsNullOrWhiteSpace(localClientId))
            {
                throw new ArgumentException("Client id must be set.", nameof(localClientId));
            }

            if (simulationTickRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(simulationTickRate), "Tick rate must be positive.");
            }

            if (playerMoveSpeed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(playerMoveSpeed), "Move speed cannot be negative.");
            }

            if (botCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(botCount), "Bot count cannot be negative.");
            }

            if (eggCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(eggCount), "Egg count cannot be negative.");
            }

            LocalClientId = localClientId;
            SimulationTickRate = simulationTickRate;
            PlayerMoveSpeed = playerMoveSpeed;
            PlayerSpawn = playerSpawn;
            BotCount = botCount;
            EggCount = eggCount;
            MatchDurationSeconds = Math.Max(0f, matchDurationSeconds);
            BotMoveSpeed = Math.Max(0f, botMoveSpeed);
            SnapshotIntervalRange = NormalizeRange(snapshotIntervalRange, 0.05f, 0.1f);
            ClientToServerLatencyRange = NormalizeRange(clientToServerLatencyRange, 0.005f, 0.02f);
            ServerToClientLatencyRange = NormalizeRange(serverToClientLatencyRange, 0.015f, 0.04f);
            SnapshotInterpolationDelay = Math.Max(0f, snapshotInterpolationDelay);
            EggCollectionRadius = Math.Max(0.01f, eggCollectionRadius);
            BotActorAvoidanceRadius = Math.Max(0f, botActorAvoidanceRadius);
            BotPathCandidateLimit = Math.Max(1, botPathCandidateLimit);
            MaxBotPathSearchesPerTick = Math.Max(1, maxBotPathSearchesPerTick);
            MaxServerTicksPerFrame = Math.Max(1, maxServerTicksPerFrame);
        }

        public static GameConfig CreateDefault()
        {
            return new GameConfig("local-player", 30, 5f, SimVector2.Zero);
        }

        private static SimVector2 NormalizeRange(SimVector2 range, float defaultMin, float defaultMax)
        {
            if (range.Equals(SimVector2.Zero))
            {
                range = new SimVector2(defaultMin, defaultMax);
            }

            float min = Math.Max(0f, Math.Min(range.X, range.Y));
            float max = Math.Max(min, Math.Max(range.X, range.Y));
            return new SimVector2(min, max);
        }
    }
}
