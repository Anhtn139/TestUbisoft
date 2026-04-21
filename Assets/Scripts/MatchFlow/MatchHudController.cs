using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace EggCollecting.MatchFlow
{
    public sealed class MatchHudController : MonoBehaviour
    {
        [SerializeField] private TMP_Text stateLabel = null;
        [SerializeField] private TMP_Text remainingTimeLabel = null;
        [SerializeField] private TMP_Text scoresLabel = null;
        [SerializeField] private TMP_Text winnerLabel = null;

        private readonly StringBuilder builder = new StringBuilder(128);

        private void OnEnable()
        {
            MatchEvents.StateChanged += HandleStateChanged;
            MatchEvents.RemainingTimeChanged += HandleRemainingTimeChanged;
            MatchEvents.ScoresChanged += HandleScoresChanged;
            MatchEvents.GameOver += HandleGameOver;
            MatchEvents.SnapshotReceived += HandleSnapshot;
            MatchEvents.RequestSnapshot();
        }

        private void OnDisable()
        {
            MatchEvents.StateChanged -= HandleStateChanged;
            MatchEvents.RemainingTimeChanged -= HandleRemainingTimeChanged;
            MatchEvents.ScoresChanged -= HandleScoresChanged;
            MatchEvents.GameOver -= HandleGameOver;
            MatchEvents.SnapshotReceived -= HandleSnapshot;
        }

        private void HandleSnapshot(MatchSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            HandleStateChanged(snapshot.State);
            HandleRemainingTimeChanged(snapshot.RemainingSeconds);
            HandleScoresChanged(snapshot.Scores);
            UpdateWinners(snapshot.State, snapshot.Winners);
        }

        private void HandleStateChanged(MatchState state)
        {
            if (stateLabel != null)
            {
                stateLabel.text = state.ToString();
            }

            if (state != MatchState.GameOver && winnerLabel != null)
            {
                winnerLabel.text = string.Empty;
            }
        }

        private void HandleRemainingTimeChanged(float remainingSeconds)
        {
            if (remainingTimeLabel == null)
            {
                return;
            }

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            remainingTimeLabel.text = $"{minutes:00}:{seconds:00}";
        }

        private void HandleScoresChanged(IReadOnlyList<PlayerScore> scores)
        {
            if (scoresLabel == null)
            {
                return;
            }

            builder.Clear();
            if (scores == null || scores.Count == 0)
            {
                builder.Append("No players");
            }
            else
            {
                for (int i = 0; i < scores.Count; i++)
                {
                    PlayerScore score = scores[i];
                    builder.Append(score.DisplayName);
                    builder.Append(": ");
                    builder.Append(score.EggCount);

                    if (i < scores.Count - 1)
                    {
                        builder.AppendLine();
                    }
                }
            }

            scoresLabel.text = builder.ToString();
        }

        private void HandleGameOver(MatchGameOverInfo gameOverInfo)
        {
            if (gameOverInfo == null)
            {
                return;
            }

            HandleScoresChanged(gameOverInfo.FinalScores);
            UpdateWinners(MatchState.GameOver, gameOverInfo.Winners);
        }

        private void UpdateWinners(MatchState state, IReadOnlyList<PlayerScore> winners)
        {
            if (winnerLabel == null)
            {
                return;
            }

            if (state != MatchState.GameOver)
            {
                winnerLabel.text = string.Empty;
                return;
            }

            if (winners == null || winners.Count == 0)
            {
                winnerLabel.text = "No winner";
                return;
            }

            builder.Clear();
            builder.Append(winners.Count == 1 ? "Winner: " : "Winners: ");
            for (int i = 0; i < winners.Count; i++)
            {
                builder.Append(winners[i].DisplayName);
                if (i < winners.Count - 1)
                {
                    builder.Append(", ");
                }
            }

            winnerLabel.text = builder.ToString();
        }
    }
}
