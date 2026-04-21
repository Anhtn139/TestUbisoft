using System.Collections.Generic;
using System.Text;
using TestUbisoft.Prototype.Client;
using TestUbisoft.Prototype.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TestUbisoft.Prototype.Presentation
{
    /// <summary>
    /// Unity scene adapter for client snapshots.
    /// It owns GameObjects and rendering concerns, while simulation rules stay in ServerSimulator.
    /// </summary>
    public sealed class UnityClientWorldView : MonoBehaviour, IClientWorldView
    {
        private static readonly Color[] EggColors =
        {
            new Color(0.95f, 0.22f, 0.18f),
            new Color(0.18f, 0.58f, 0.96f),
            new Color(0.98f, 0.78f, 0.22f),
            new Color(0.35f, 0.78f, 0.34f),
            new Color(0.88f, 0.36f, 0.96f),
            new Color(0.96f, 0.52f, 0.25f)
        };

        [SerializeField] private PlayerView playerPrefab = null;
        [SerializeField] private PlayerView botPrefab = null;
        [SerializeField] private GameObject eggPrefab = null;
        [SerializeField] private Transform actorRoot = null;
        [SerializeField] private Transform eggRoot = null;
        [SerializeField] private string localPlayerId = "local-player";
        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI stateLabel = null;
        [SerializeField] private TextMeshProUGUI objectiveLabel = null;
        [SerializeField] private TextMeshProUGUI remainingTimeLabel = null;
        [SerializeField] private TextMeshProUGUI scoresLabel = null;
        [SerializeField] private TextMeshProUGUI winnerLabel = null;
        [SerializeField] private Button replayButton = null;
        [SerializeField] private TextMeshProUGUI replayButtonLabel = null;

        private readonly Dictionary<int, PlayerView> _actorsByEntityId = new Dictionary<int, PlayerView>();
        private readonly Dictionary<int, GameObject> _eggsByEntityId = new Dictionary<int, GameObject>();
        private readonly HashSet<int> _actorsSeenThisSnapshot = new HashSet<int>();
        private readonly HashSet<int> _eggsSeenThisSnapshot = new HashSet<int>();
        private readonly List<ScoreSnapshot> _sortedScores = new List<ScoreSnapshot>(8);
        private readonly StringBuilder _scoreBuilder = new StringBuilder(128);

        private WorldSnapshot _latestSnapshot;
        private RectTransform _hudPanel;

        private void Awake()
        {
            EnsureHudBindings();
            RegisterReplayButton();
        }

        public void Render(WorldSnapshot snapshot)
        {
            EnsureHudBindings();
            _latestSnapshot = snapshot;
            _actorsSeenThisSnapshot.Clear();
            _eggsSeenThisSnapshot.Clear();
            UpdateHud(snapshot);

            foreach (EntitySnapshot entity in snapshot.Entities)
            {
                if (entity.Kind == EntityKind.Egg)
                {
                    RenderEgg(entity);
                    continue;
                }

                if (entity.Kind != EntityKind.Player && entity.Kind != EntityKind.Bot)
                {
                    continue;
                }

                _actorsSeenThisSnapshot.Add(entity.EntityId);
                PlayerView view = GetOrCreateActorView(entity);
                view.ApplySnapshot(entity.Position.ToUnityPosition(1f), entity.YawDegrees.ToUnityYaw());
            }

            RemoveStaleActorViews();
            RemoveStaleEggViews();
        }

        public void Clear()
        {
            foreach (PlayerView view in _actorsByEntityId.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            foreach (GameObject egg in _eggsByEntityId.Values)
            {
                if (egg != null)
                {
                    Destroy(egg);
                }
            }

            _actorsByEntityId.Clear();
            _eggsByEntityId.Clear();
            _actorsSeenThisSnapshot.Clear();
            _eggsSeenThisSnapshot.Clear();
            _latestSnapshot = null;
        }

        private void UpdateHud(WorldSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            SortScores(snapshot.Scores);

            if (stateLabel != null)
            {
                stateLabel.text = GetStateText(snapshot);
            }

            if (objectiveLabel != null)
            {
                objectiveLabel.text = "Collect the most eggs";
            }

            if (remainingTimeLabel != null)
            {
                remainingTimeLabel.text = FormatRemainingTime(snapshot.RemainingSeconds);
            }

            if (scoresLabel != null)
            {
                scoresLabel.text = BuildScoreboardText();
            }

            if (winnerLabel != null)
            {
                winnerLabel.text = IsGameOver(snapshot) ? GetWinnerText(snapshot) : string.Empty;
            }

            bool isGameOver = IsGameOver(snapshot);
            if (replayButton != null)
            {
                replayButton.gameObject.SetActive(isGameOver);
            }

            if (_hudPanel != null)
            {
                float height = isGameOver ? 282f : 188f;
                height += Mathf.Max(1, _sortedScores.Count) * 28f;
                _hudPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
        }

        private bool HasBoundHudText()
        {
            return stateLabel != null
                || objectiveLabel != null
                || remainingTimeLabel != null
                || scoresLabel != null
                || winnerLabel != null
                || replayButton != null;
        }

        private void EnsureHudBindings()
        {
            if (HasBoundHudText())
            {
                return;
            }

            Canvas existingCanvas = GetComponentInChildren<Canvas>(true);
            if (existingCanvas != null)
            {
                BindExistingHud(existingCanvas.transform);
            }
        }

        private void BindExistingHud(Transform root)
        {
            if (root == null)
            {
                return;
            }

            if (stateLabel == null)
            {
                stateLabel = FindLabel(root, "StateLabel");
            }

            if (objectiveLabel == null)
            {
                objectiveLabel = FindLabel(root, "ObjectiveLabel");
            }

            if (remainingTimeLabel == null)
            {
                remainingTimeLabel = FindLabel(root, "RemainingTimeLabel");
            }

            if (scoresLabel == null)
            {
                scoresLabel = FindLabel(root, "ScoresLabel");
            }

            if (winnerLabel == null)
            {
                winnerLabel = FindLabel(root, "WinnerLabel");
            }

            if (replayButton == null)
            {
                replayButton = FindButton(root, "ReplayButton");
            }

            if (replayButtonLabel == null)
            {
                replayButtonLabel = FindLabel(root, "ReplayButtonLabel");
            }

            if (_hudPanel == null)
            {
                Transform panel = root.Find("HudPanel");
                if (panel != null)
                {
                    _hudPanel = panel as RectTransform;
                }
            }
        }

        private static TextMeshProUGUI FindLabel(Transform root, string childName)
        {
            Transform child = root.Find(childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private static Button FindButton(Transform root, string childName)
        {
            Transform child = root.Find($"HudPanel/{childName}");
            return child != null ? child.GetComponent<Button>() : null;
        }

        private void RegisterReplayButton()
        {
            if (replayButton == null)
            {
                return;
            }

            replayButton.onClick.RemoveListener(HandleReplayButtonClicked);
            replayButton.onClick.AddListener(HandleReplayButtonClicked);
        }

        private static void HandleReplayButtonClicked()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                return;
            }

            SceneManager.LoadScene(activeScene.buildIndex);
        }

        private void SortScores(IReadOnlyList<ScoreSnapshot> scores)
        {
            _sortedScores.Clear();
            if (scores == null)
            {
                return;
            }

            for (int i = 0; i < scores.Count; i++)
            {
                _sortedScores.Add(scores[i]);
            }

            _sortedScores.Sort(CompareScores);
        }

        private string BuildScoreboardText()
        {
            _scoreBuilder.Clear();
            if (_sortedScores.Count == 0)
            {
                _scoreBuilder.Append("No players");
                return _scoreBuilder.ToString();
            }

            _scoreBuilder.Append("<mspace=0.72em><b>#  NAME              EGGS</b></mspace>");
            _scoreBuilder.AppendLine();

            for (int i = 0; i < _sortedScores.Count; i++)
            {
                ScoreSnapshot score = _sortedScores[i];
                string rank = (i + 1).ToString().PadRight(3);
                string name = score.DisplayName.ToUpperInvariant();
                if (name.Length > 16)
                {
                    name = name.Substring(0, 16);
                }

                name = name.PadRight(16);
                string eggs = score.EggCount.ToString().PadLeft(4);
                bool isLocalPlayer = string.Equals(score.PlayerId, localPlayerId, System.StringComparison.Ordinal);

                if (isLocalPlayer)
                {
                    _scoreBuilder.Append("<color=#7FDBFF>");
                }

                _scoreBuilder.Append("<mspace=0.72em>");
                _scoreBuilder.Append(rank);
                _scoreBuilder.Append(name);
                _scoreBuilder.Append(eggs);
                _scoreBuilder.Append("</mspace>");

                if (isLocalPlayer)
                {
                    _scoreBuilder.Append("</color>");
                }

                if (i < _sortedScores.Count - 1)
                {
                    _scoreBuilder.AppendLine();
                }
            }

            return _scoreBuilder.ToString();
        }

        private static bool IsGameOver(WorldSnapshot snapshot)
        {
            return snapshot != null && snapshot.RemainingSeconds <= 0.01f;
        }

        private static string GetStateText(WorldSnapshot snapshot)
        {
            return IsGameOver(snapshot) ? "Game Over" : "Playing";
        }

        private string GetWinnerText(WorldSnapshot snapshot)
        {
            if (_sortedScores.Count == 0)
            {
                return "No winner";
            }

            int winningScore = _sortedScores[0].EggCount;
            int winnerCount = 0;
            for (int i = 0; i < _sortedScores.Count; i++)
            {
                if (_sortedScores[i].EggCount != winningScore)
                {
                    break;
                }

                winnerCount++;
            }

            _scoreBuilder.Clear();
            _scoreBuilder.Append(winnerCount == 1 ? "Winner: " : "Winners: ");
            for (int i = 0; i < winnerCount; i++)
            {
                _scoreBuilder.Append(_sortedScores[i].DisplayName);
                if (i < winnerCount - 1)
                {
                    _scoreBuilder.Append(", ");
                }
            }

            return _scoreBuilder.ToString();
        }

        private static string FormatRemainingTime(float remainingSeconds)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        private static int CompareScores(ScoreSnapshot left, ScoreSnapshot right)
        {
            int eggCompare = right.EggCount.CompareTo(left.EggCount);
            if (eggCompare != 0)
            {
                return eggCompare;
            }

            return string.CompareOrdinal(left.DisplayName, right.DisplayName);
        }

        private void RenderEgg(EntitySnapshot entity)
        {
            if (!entity.IsActive)
            {
                if (_eggsByEntityId.TryGetValue(entity.EntityId, out GameObject inactiveEgg) && inactiveEgg != null)
                {
                    Destroy(inactiveEgg);
                }

                _eggsByEntityId.Remove(entity.EntityId);
                return;
            }

            _eggsSeenThisSnapshot.Add(entity.EntityId);

            GameObject egg = GetOrCreateEggView(entity);
            egg.transform.position = entity.Position.ToUnityPosition(0.35f);
            egg.transform.rotation = Quaternion.identity;
        }

        private PlayerView GetOrCreateActorView(EntitySnapshot entity)
        {
            if (_actorsByEntityId.TryGetValue(entity.EntityId, out PlayerView existingView) && existingView != null)
            {
                return existingView;
            }

            PlayerView prefab = entity.Kind == EntityKind.Bot ? botPrefab : playerPrefab;
            PlayerView view = prefab != null
                ? Instantiate(prefab, actorRoot != null ? actorRoot : transform)
                : CreateFallbackActorView(entity);

            bool isLocalPlayer = entity.Kind == EntityKind.Player
                && string.Equals(entity.OwnerClientId, localPlayerId, System.StringComparison.Ordinal);
            view.Initialize(entity.EntityId, isLocalPlayer);
            view.name = entity.Kind == EntityKind.Bot ? $"BotView_{entity.EntityId}" : $"PlayerView_{entity.EntityId}";
            _actorsByEntityId[entity.EntityId] = view;
            return view;
        }

        private PlayerView CreateFallbackActorView(EntitySnapshot entity)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(actorRoot != null ? actorRoot : transform, false);
            visual.name = entity.Kind == EntityKind.Bot ? $"BotView_{entity.EntityId}" : $"PlayerView_{entity.EntityId}";

            Renderer renderer = visual.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = entity.Kind == EntityKind.Bot
                    ? new Color(0.9f, 0.36f, 0.2f)
                    : new Color(0.2f, 0.5f, 0.95f);
            }

            return visual.AddComponent<PlayerView>();
        }

        private GameObject GetOrCreateEggView(EntitySnapshot entity)
        {
            if (_eggsByEntityId.TryGetValue(entity.EntityId, out GameObject existingEgg) && existingEgg != null)
            {
                return existingEgg;
            }

            GameObject egg = eggPrefab != null
                ? Instantiate(eggPrefab, eggRoot != null ? eggRoot : transform)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);

            egg.transform.SetParent(eggRoot != null ? eggRoot : transform, false);
            egg.name = $"EggView_{entity.EntityId}";
            egg.transform.localScale = new Vector3(0.45f, 0.65f, 0.45f);

            Collider collider = egg.GetComponentInChildren<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            Renderer renderer = egg.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = EggColors[Mathf.Abs(entity.VisualIndex) % EggColors.Length];
            }

            _eggsByEntityId[entity.EntityId] = egg;
            return egg;
        }

        private void RemoveStaleActorViews()
        {
            List<int> staleEntityIds = null;
            foreach (int entityId in _actorsByEntityId.Keys)
            {
                if (!_actorsSeenThisSnapshot.Contains(entityId))
                {
                    staleEntityIds ??= new List<int>();
                    staleEntityIds.Add(entityId);
                }
            }

            if (staleEntityIds == null)
            {
                return;
            }

            foreach (int entityId in staleEntityIds)
            {
                if (_actorsByEntityId.TryGetValue(entityId, out PlayerView view) && view != null)
                {
                    Destroy(view.gameObject);
                }

                _actorsByEntityId.Remove(entityId);
            }
        }

        private void RemoveStaleEggViews()
        {
            List<int> staleEntityIds = null;
            foreach (int entityId in _eggsByEntityId.Keys)
            {
                if (!_eggsSeenThisSnapshot.Contains(entityId))
                {
                    staleEntityIds ??= new List<int>();
                    staleEntityIds.Add(entityId);
                }
            }

            if (staleEntityIds == null)
            {
                return;
            }

            foreach (int entityId in staleEntityIds)
            {
                if (_eggsByEntityId.TryGetValue(entityId, out GameObject egg) && egg != null)
                {
                    Destroy(egg);
                }

                _eggsByEntityId.Remove(entityId);
            }
        }
    }
}
