using System;
using System.Collections.Generic;
using UnityEngine;

namespace EggCollecting.MatchFlow
{
    public sealed class MatchFlowController : MonoBehaviour
    {
        private sealed class PlayerRecord
        {
            public PlayerRecord(PlayerIdentity identity)
            {
                PlayerId = identity.PlayerId;
                DisplayName = identity.DisplayName;
            }

            public string PlayerId { get; }
            public string DisplayName { get; private set; }
            public int EggCount { get; set; }

            public void Refresh(PlayerIdentity identity)
            {
                DisplayName = identity.DisplayName;
            }

            public PlayerScore ToScore()
            {
                return new PlayerScore(PlayerId, DisplayName, EggCount);
            }
        }

        [Header("Authority")]
        [SerializeField] private bool actAsServer = true;
        [SerializeField] private bool startPlayingOnStart = true;

        [Header("Match")]
        [SerializeField] private float matchDurationSeconds = 120f;
        [SerializeField] private float timerBroadcastInterval = 0.2f;
        [SerializeField] private List<PlayerIdentity> startingPlayers = new List<PlayerIdentity>();

        private readonly Dictionary<string, PlayerRecord> players = new Dictionary<string, PlayerRecord>();
        private readonly HashSet<EggPickup> collectedEggs = new HashSet<EggPickup>();
        private readonly List<PlayerScore> cachedWinners = new List<PlayerScore>();

        private double serverEndsAt;
        private float nextTimerBroadcastAt;

        public static MatchFlowController Active { get; private set; }

        public MatchState State { get; private set; } = MatchState.Boot;
        public bool IsServer => actAsServer;
        public float RemainingSeconds => CalculateRemainingSeconds();

        private void Awake()
        {
            if (Active != null && Active != this)
            {
                Debug.LogWarning("Multiple MatchFlowController instances found. Disabling duplicate.", this);
                enabled = false;
                return;
            }

            Active = this;
        }

        private void OnEnable()
        {
            MatchEvents.SnapshotRequested += PublishSnapshot;
        }

        private void Start()
        {
            RegisterConfiguredPlayers();
            RegisterScenePlayers();
            SetState(MatchState.Boot);
            PublishFullState();

            if (startPlayingOnStart)
            {
                ServerStartMatch();
            }
        }

        private void Update()
        {
            if (!actAsServer || State != MatchState.Playing)
            {
                return;
            }

            if (Time.timeAsDouble >= serverEndsAt)
            {
                ServerEndMatch();
                return;
            }

            if (Time.unscaledTime >= nextTimerBroadcastAt)
            {
                PublishRemainingTime();
                nextTimerBroadcastAt = Time.unscaledTime + Mathf.Max(0.05f, timerBroadcastInterval);
            }
        }

        private void OnDisable()
        {
            MatchEvents.SnapshotRequested -= PublishSnapshot;

            if (Active == this)
            {
                Active = null;
            }
        }

        public void ServerStartMatch()
        {
            if (!EnsureServer())
            {
                return;
            }

            RegisterConfiguredPlayers();
            RegisterScenePlayers();
            ResetScores();
            ResetEggs();
            cachedWinners.Clear();

            serverEndsAt = Time.timeAsDouble + Mathf.Max(0f, matchDurationSeconds);
            nextTimerBroadcastAt = Time.unscaledTime;

            SetState(MatchState.Playing);
            PublishFullState();
        }

        public bool ServerRegisterPlayer(PlayerIdentity identity)
        {
            if (!EnsureServer(false) || identity == null)
            {
                return false;
            }

            PlayerRecord record;
            if (players.TryGetValue(identity.PlayerId, out record))
            {
                record.Refresh(identity);
            }
            else
            {
                players.Add(identity.PlayerId, new PlayerRecord(identity));
            }

            PublishScores();
            PublishSnapshot();
            return true;
        }

        public bool ServerResolveEggCollection(PlayerIdentity player, EggPickup egg)
        {
            if (!EnsureServer() || State != MatchState.Playing || player == null || egg == null)
            {
                return false;
            }

            if (egg.IsCollected || collectedEggs.Contains(egg))
            {
                return false;
            }

            ServerRegisterPlayer(player);

            PlayerRecord record;
            if (!players.TryGetValue(player.PlayerId, out record))
            {
                return false;
            }

            collectedEggs.Add(egg);
            record.EggCount++;
            egg.ServerMarkCollected();

            IReadOnlyList<PlayerScore> scores = BuildScoreboard();
            MatchEvents.RaiseEggCollected(player.PlayerId, egg.EggId);
            MatchEvents.RaiseScoresChanged(scores);
            PublishSnapshot(scores);
            return true;
        }

        public void ServerEndMatch()
        {
            if (!EnsureServer() || State == MatchState.GameOver)
            {
                return;
            }

            SetState(MatchState.GameOver);

            IReadOnlyList<PlayerScore> finalScores = BuildScoreboard();
            IReadOnlyList<PlayerScore> winners = BuildWinners(finalScores);
            MatchGameOverInfo gameOverInfo = new MatchGameOverInfo(finalScores, winners);

            MatchEvents.RaiseRemainingTimeChanged(0f);
            MatchEvents.RaiseScoresChanged(finalScores);
            MatchEvents.RaiseGameOver(gameOverInfo);
            PublishSnapshot(finalScores, winners);
        }

        public void ServerReturnToBoot()
        {
            if (!EnsureServer())
            {
                return;
            }

            cachedWinners.Clear();
            SetState(MatchState.Boot);
            PublishFullState();
        }

        private bool EnsureServer(bool logWarning = true)
        {
            if (actAsServer)
            {
                return true;
            }

            if (logWarning)
            {
                Debug.LogWarning("Ignoring server-only match operation on a non-authoritative client.", this);
            }

            return false;
        }

        private void RegisterConfiguredPlayers()
        {
            for (int i = 0; i < startingPlayers.Count; i++)
            {
                if (startingPlayers[i] != null)
                {
                    ServerRegisterPlayer(startingPlayers[i]);
                }
            }
        }

        private void RegisterScenePlayers()
        {
            PlayerIdentity[] scenePlayers = FindObjectsOfType<PlayerIdentity>();
            for (int i = 0; i < scenePlayers.Length; i++)
            {
                ServerRegisterPlayer(scenePlayers[i]);
            }
        }

        private void ResetScores()
        {
            foreach (PlayerRecord record in players.Values)
            {
                record.EggCount = 0;
            }
        }

        private void ResetEggs()
        {
            collectedEggs.Clear();

            EggPickup[] eggs = FindObjectsOfType<EggPickup>(true);
            for (int i = 0; i < eggs.Length; i++)
            {
                eggs[i].ServerResetForMatch();
            }
        }

        private void SetState(MatchState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            MatchEvents.RaiseStateChanged(State);
        }

        private void PublishFullState()
        {
            IReadOnlyList<PlayerScore> scores = BuildScoreboard();
            PublishRemainingTime();
            MatchEvents.RaiseScoresChanged(scores);
            PublishSnapshot(scores);
        }

        private void PublishRemainingTime()
        {
            MatchEvents.RaiseRemainingTimeChanged(CalculateRemainingSeconds());
        }

        private void PublishScores()
        {
            MatchEvents.RaiseScoresChanged(BuildScoreboard());
        }

        private void PublishSnapshot()
        {
            PublishSnapshot(BuildScoreboard(), cachedWinners);
        }

        private void PublishSnapshot(IReadOnlyList<PlayerScore> scores)
        {
            PublishSnapshot(scores, cachedWinners);
        }

        private void PublishSnapshot(IReadOnlyList<PlayerScore> scores, IReadOnlyList<PlayerScore> winners)
        {
            MatchEvents.RaiseSnapshot(new MatchSnapshot(State, CalculateRemainingSeconds(), scores, winners));
        }

        private float CalculateRemainingSeconds()
        {
            if (State == MatchState.GameOver)
            {
                return 0f;
            }

            if (State != MatchState.Playing)
            {
                return Mathf.Max(0f, matchDurationSeconds);
            }

            return Mathf.Max(0f, (float)(serverEndsAt - Time.timeAsDouble));
        }

        private IReadOnlyList<PlayerScore> BuildScoreboard()
        {
            List<PlayerScore> scores = new List<PlayerScore>(players.Count);
            foreach (PlayerRecord record in players.Values)
            {
                scores.Add(record.ToScore());
            }

            scores.Sort(CompareScores);
            return scores.AsReadOnly();
        }

        private IReadOnlyList<PlayerScore> BuildWinners(IReadOnlyList<PlayerScore> finalScores)
        {
            cachedWinners.Clear();

            if (finalScores == null || finalScores.Count == 0)
            {
                return cachedWinners.AsReadOnly();
            }

            int winningScore = finalScores[0].EggCount;
            for (int i = 0; i < finalScores.Count; i++)
            {
                if (finalScores[i].EggCount != winningScore)
                {
                    break;
                }

                cachedWinners.Add(finalScores[i]);
            }

            return cachedWinners.AsReadOnly();
        }

        private static int CompareScores(PlayerScore left, PlayerScore right)
        {
            int scoreComparison = right.EggCount.CompareTo(left.EggCount);
            if (scoreComparison != 0)
            {
                return scoreComparison;
            }

            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
        }
    }
}
