using System;
using System.Collections.Generic;

namespace TestUbisoft.Prototype.Core
{
    /// <summary>
    /// Immutable client-facing view of one server tick.
    /// This is the contract a real server would eventually serialize over the network.
    /// </summary>
    public sealed class WorldSnapshot
    {
        private static readonly EntitySnapshot[] EmptyEntities = Array.Empty<EntitySnapshot>();
        private static readonly ScoreSnapshot[] EmptyScores = Array.Empty<ScoreSnapshot>();

        private readonly EntitySnapshot[] _entities;
        private readonly ScoreSnapshot[] _scores;

        public int Tick { get; }
        public double ServerTime { get; }
        public float RemainingSeconds { get; }
        public IReadOnlyList<EntitySnapshot> Entities => _entities;
        public IReadOnlyList<ScoreSnapshot> Scores => _scores;

        public WorldSnapshot(
            int tick,
            double serverTime,
            float remainingSeconds,
            IReadOnlyList<EntitySnapshot> entities,
            IReadOnlyList<ScoreSnapshot> scores)
        {
            Tick = tick;
            ServerTime = serverTime;
            RemainingSeconds = remainingSeconds;

            if (entities == null || entities.Count == 0)
            {
                _entities = EmptyEntities;
            }
            else
            {
                _entities = new EntitySnapshot[entities.Count];
                for (int i = 0; i < entities.Count; i++)
                {
                    _entities[i] = entities[i];
                }
            }

            if (scores == null || scores.Count == 0)
            {
                _scores = EmptyScores;
            }
            else
            {
                _scores = new ScoreSnapshot[scores.Count];
                for (int i = 0; i < scores.Count; i++)
                {
                    _scores[i] = scores[i];
                }
            }
        }
    }
}
