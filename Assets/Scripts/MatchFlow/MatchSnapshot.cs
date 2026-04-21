using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EggCollecting.MatchFlow
{
    public sealed class MatchSnapshot
    {
        public MatchSnapshot(
            MatchState state,
            float remainingSeconds,
            IReadOnlyList<PlayerScore> scores,
            IReadOnlyList<PlayerScore> winners)
        {
            State = state;
            RemainingSeconds = remainingSeconds;
            Scores = Copy(scores);
            Winners = Copy(winners);
        }

        public MatchState State { get; }
        public float RemainingSeconds { get; }
        public IReadOnlyList<PlayerScore> Scores { get; }
        public IReadOnlyList<PlayerScore> Winners { get; }

        private static ReadOnlyCollection<PlayerScore> Copy(IReadOnlyList<PlayerScore> source)
        {
            if (source == null || source.Count == 0)
            {
                return new ReadOnlyCollection<PlayerScore>(new List<PlayerScore>());
            }

            List<PlayerScore> copy = new List<PlayerScore>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                copy.Add(source[i]);
            }

            return new ReadOnlyCollection<PlayerScore>(copy);
        }
    }
}
