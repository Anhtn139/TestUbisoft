#if UNITY_SERVER || UNITY_EDITOR
using UnityEngine;

namespace TestUbisoft.Server.AI
{
    [DisallowMultipleComponent]
    public sealed class ServerEggBotController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private MonoBehaviour eggProviderBehaviour;
        [SerializeField] private MonoBehaviour pathfinderBehaviour;
        [SerializeField] private MonoBehaviour movementTargetSinkBehaviour;
        [SerializeField] private EggTargetSelectionStrategy targetSelectionStrategy;

        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float evaluationInterval = 0.6f;
        [SerializeField] private Vector2 evaluationJitterSeconds = new Vector2(0.05f, 0.2f);
        [SerializeField] private Vector2 reactionDelaySeconds = new Vector2(0.08f, 0.25f);

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float waypointReachedDistance = 0.15f;
        [SerializeField] private bool clearMovementWhenNoTarget = true;

        private IEggProvider eggProvider;
        private IPathfinder pathfinder;
        private IServerMovementTargetSink movementTargetSink;
        private IEggTargetSelectionStrategy selectionStrategy;

        private EggTarget currentTarget;
        private PathResult currentPath;
        private int currentWaypointIndex;
        private bool hasTarget;
        private bool repathQueued;
        private bool warnedAboutDependencies;
        private float nextEvaluationTime;
        private float queuedRepathTime;
        private System.Random random;

        private LowestPathCostEggSelectionStrategy defaultSelectionStrategy;

        public void ConfigureDependencies(
            MonoBehaviour eggProvider,
            MonoBehaviour pathfinder,
            MonoBehaviour movementTargetSink)
        {
            eggProviderBehaviour = eggProvider != null ? eggProvider : eggProviderBehaviour;
            pathfinderBehaviour = pathfinder != null ? pathfinder : pathfinderBehaviour;
            movementTargetSinkBehaviour = movementTargetSink != null ? movementTargetSink : movementTargetSinkBehaviour;
            warnedAboutDependencies = false;
            ResolveDependencies();
            ResolveSelectionStrategy();
        }

        public void RequestRepath()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            repathQueued = true;
            queuedRepathTime = Time.time;
        }

        private void Awake()
        {
            random = new System.Random(unchecked((System.Environment.TickCount * 397) ^ GetInstanceID()));
            ResolveDependencies();
            ResolveSelectionStrategy();
        }

        private void OnEnable()
        {
            ScheduleNextEvaluation(Time.time, true);
        }

        private void OnDisable()
        {
            ClearTarget();
        }

        private void OnDestroy()
        {
            if (defaultSelectionStrategy != null)
            {
                Destroy(defaultSelectionStrategy);
                defaultSelectionStrategy = null;
            }
        }

        private void OnValidate()
        {
            evaluationInterval = Mathf.Max(0.05f, evaluationInterval);
            waypointReachedDistance = Mathf.Max(0.01f, waypointReachedDistance);
            evaluationJitterSeconds = NormalizeRange(evaluationJitterSeconds);
            reactionDelaySeconds = NormalizeRange(reactionDelaySeconds);
        }

        private void Update()
        {
            if (!HasRequiredDependencies())
            {
                WarnAboutMissingDependencies();
                return;
            }

            var now = Time.time;

            if (hasTarget && CurrentTargetNeedsRepath())
            {
                QueueRepath(now);
            }

            if (repathQueued && now >= queuedRepathTime)
            {
                EvaluateTarget(now);
            }
            else if (now >= nextEvaluationTime)
            {
                QueueRepath(now);
            }

            if (hasTarget)
            {
                OutputCurrentMovementTarget();
            }
        }

        private void ResolveDependencies()
        {
            eggProvider = ResolveInterface<IEggProvider>(eggProviderBehaviour) ?? ResolveSceneInterface<IEggProvider>();
            pathfinder = ResolveInterface<IPathfinder>(pathfinderBehaviour) ?? ResolveSceneInterface<IPathfinder>();
            movementTargetSink = ResolveInterface<IServerMovementTargetSink>(movementTargetSinkBehaviour);
        }

        private void ResolveSelectionStrategy()
        {
            if (targetSelectionStrategy != null)
            {
                selectionStrategy = targetSelectionStrategy;
                return;
            }

            defaultSelectionStrategy = ScriptableObject.CreateInstance<LowestPathCostEggSelectionStrategy>();
            defaultSelectionStrategy.hideFlags = HideFlags.HideAndDontSave;
            selectionStrategy = defaultSelectionStrategy;
        }

        private T ResolveInterface<T>(MonoBehaviour explicitBehaviour) where T : class
        {
            if (explicitBehaviour is T explicitInterface)
            {
                return explicitInterface;
            }

            var behaviours = GetComponents<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is T resolvedInterface)
                {
                    return resolvedInterface;
                }
            }

            return null;
        }

        private static T ResolveSceneInterface<T>() where T : class
        {
            var behaviours = FindObjectsOfType<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is T resolvedInterface)
                {
                    return resolvedInterface;
                }
            }

            return null;
        }

        private bool HasRequiredDependencies()
        {
            return eggProvider != null
                && pathfinder != null
                && movementTargetSink != null
                && selectionStrategy != null;
        }

        private void WarnAboutMissingDependencies()
        {
            if (warnedAboutDependencies)
            {
                return;
            }

            warnedAboutDependencies = true;
            Debug.LogWarning(
                $"{nameof(ServerEggBotController)} requires components implementing "
                + $"{nameof(IEggProvider)}, {nameof(IPathfinder)}, and {nameof(IServerMovementTargetSink)}.",
                this);
        }

        private bool CurrentTargetNeedsRepath()
        {
            if (!currentTarget.IsValid || !eggProvider.TryGetEgg(currentTarget.Id, out var latestEgg) || !latestEgg.IsValid)
            {
                return true;
            }

            if ((latestEgg.Position - currentTarget.Position).sqrMagnitude > 0.0001f)
            {
                return true;
            }

            return currentPath.IsBlocked || pathfinder.IsPathBlocked(currentPath);
        }

        private void QueueRepath(float now)
        {
            if (repathQueued)
            {
                return;
            }

            repathQueued = true;
            queuedRepathTime = now + RandomRange(reactionDelaySeconds.x, reactionDelaySeconds.y);
        }

        private void EvaluateTarget(float now)
        {
            repathQueued = false;
            ScheduleNextEvaluation(now, false);

            var context = new BotTargetSelectionContext(transform.position, eggProvider, pathfinder);
            if (!selectionStrategy.TrySelectTarget(context, out var selection))
            {
                ClearTarget();
                return;
            }

            currentTarget = selection.Target;
            currentPath = selection.Path;
            currentWaypointIndex = 0;
            hasTarget = true;
        }

        private void OutputCurrentMovementTarget()
        {
            AdvanceWaypointIfNeeded();
            movementTargetSink.SetMovementTarget(currentPath.GetWaypointOrDestination(currentWaypointIndex));
        }

        private void AdvanceWaypointIfNeeded()
        {
            var waypointCount = currentPath.WaypointCount;
            if (waypointCount <= 1)
            {
                return;
            }

            var reachedDistanceSqr = waypointReachedDistance * waypointReachedDistance;
            while (currentWaypointIndex < waypointCount - 1)
            {
                var waypoint = currentPath.GetWaypointOrDestination(currentWaypointIndex);
                if (GetPlanarDistanceSqr(waypoint, transform.position) > reachedDistanceSqr)
                {
                    break;
                }

                currentWaypointIndex++;
            }
        }

        private static float GetPlanarDistanceSqr(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return (dx * dx) + (dz * dz);
        }

        private void ClearTarget()
        {
            hasTarget = false;
            repathQueued = false;
            currentWaypointIndex = 0;
            currentTarget = default;
            currentPath = default;

            if (clearMovementWhenNoTarget && movementTargetSink != null)
            {
                movementTargetSink.ClearMovementTarget();
            }
        }

        private void ScheduleNextEvaluation(float now, bool includeInitialDelay)
        {
            var baseDelay = includeInitialDelay ? RandomRange(reactionDelaySeconds.x, reactionDelaySeconds.y) : evaluationInterval;
            nextEvaluationTime = now + baseDelay + RandomRange(evaluationJitterSeconds.x, evaluationJitterSeconds.y);
        }

        private float RandomRange(float min, float max)
        {
            if (max <= min)
            {
                return min;
            }

            if (random == null)
            {
                random = new System.Random(GetInstanceID());
            }

            return min + (float)random.NextDouble() * (max - min);
        }

        private static Vector2 NormalizeRange(Vector2 range)
        {
            range.x = Mathf.Max(0f, range.x);
            range.y = Mathf.Max(0f, range.y);

            if (range.y < range.x)
            {
                range.y = range.x;
            }

            return range;
        }
    }
}
#endif
