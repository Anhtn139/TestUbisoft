using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EggCollecting.MatchFlow
{
    public sealed class MatchGameOverInfo
    {
        public MatchGameOverInfo(IReadOnlyList<PlayerScore> finalScores, IReadOnlyList<PlayerScore> winners)
        {
            FinalScores = Copy(finalScores);
            Winners = Copy(winners);
        }

        public IReadOnlyList<PlayerScore> FinalScores { get; }
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
