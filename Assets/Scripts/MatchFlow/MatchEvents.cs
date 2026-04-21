using System;
using System.Collections.Generic;
using UnityEngine;

namespace EggCollecting.MatchFlow
{
    public static class MatchEvents
    {
        public static event Action<MatchState> StateChanged;
        public static event Action<float> RemainingTimeChanged;
        public static event Action<IReadOnlyList<PlayerScore>> ScoresChanged;
        public static event Action<string, string> EggCollected;
        public static event Action<MatchGameOverInfo> GameOver;
        public static event Action<MatchSnapshot> SnapshotReceived;

        internal static event Action SnapshotRequested;

        public static void RequestSnapshot()
        {
            SnapshotRequested?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            StateChanged = null;
            RemainingTimeChanged = null;
            ScoresChanged = null;
            EggCollected = null;
            GameOver = null;
            SnapshotReceived = null;
            SnapshotRequested = null;
        }

        internal static void RaiseStateChanged(MatchState state)
        {
            StateChanged?.Invoke(state);
        }

        internal static void RaiseRemainingTimeChanged(float remainingSeconds)
        {
            RemainingTimeChanged?.Invoke(remainingSeconds);
        }

        internal static void RaiseScoresChanged(IReadOnlyList<PlayerScore> scores)
        {
            ScoresChanged?.Invoke(scores);
        }

        internal static void RaiseEggCollected(string playerId, string eggId)
        {
            EggCollected?.Invoke(playerId, eggId);
        }

        internal static void RaiseGameOver(MatchGameOverInfo gameOverInfo)
        {
            GameOver?.Invoke(gameOverInfo);
        }

        internal static void RaiseSnapshot(MatchSnapshot snapshot)
        {
            SnapshotReceived?.Invoke(snapshot);
        }
    }
}
