#if UNITY_SERVER || UNITY_EDITOR
using UnityEngine;
using TestUbisoft.Prototype.Presentation;

namespace TestUbisoft.Server.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ServerMovementTargetOutput))]
    public sealed class BotGridMover : MonoBehaviour
    {
        private const int ActorLayer = 7;

        [SerializeField, Min(0.01f)] private float moveSpeed = 3.5f;
        [SerializeField, Min(1f)] private float turnSpeedDegrees = 720f;
        [SerializeField, Min(0.001f)] private float stopDistance = 0.05f;
        [SerializeField] private bool useCollision = true;
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField, Min(0f)] private float collisionSkinWidth = 0.03f;
        [SerializeField, Min(0.001f)] private float minimumMoveDistance = 0.01f;
        [SerializeField, Min(0.05f)] private float repathCooldown = 0.35f;

        private ServerMovementTargetOutput movementTargetOutput;
        private ServerEggBotController botController;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private Vector3 movementTarget;
        private bool hasMovementTarget;
        private float nextRepathRequestTime;

        private static readonly float[] AvoidanceAngles =
        {
            35f,
            -35f,
            70f,
            -70f,
            110f,
            -110f,
            150f,
            -150f
        };

        private void Awake()
        {
            SetLayerRecursively(transform, ActorLayer);
            movementTargetOutput = GetComponent<ServerMovementTargetOutput>();
            botController = GetComponent<ServerEggBotController>();
            ResolvePhysicsComponents();
        }

        private void OnEnable()
        {
            if (movementTargetOutput == null)
            {
                movementTargetOutput = GetComponent<ServerMovementTargetOutput>();
            }

            movementTargetOutput.MovementTargetChanged += HandleMovementTargetChanged;
            movementTargetOutput.MovementTargetCleared += HandleMovementTargetCleared;

            if (movementTargetOutput.HasMovementTarget)
            {
                HandleMovementTargetChanged(movementTargetOutput.MovementTarget);
            }
            else
            {
                hasMovementTarget = false;
            }
        }

        private void OnDisable()
        {
            if (movementTargetOutput == null)
            {
                return;
            }

            movementTargetOutput.MovementTargetChanged -= HandleMovementTargetChanged;
            movementTargetOutput.MovementTargetCleared -= HandleMovementTargetCleared;
        }

        private void FixedUpdate()
        {
            if (!hasMovementTarget)
            {
                return;
            }

            Vector3 toTarget = movementTarget - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
            {
                return;
            }

            Vector3 direction = toTarget.normalized;
            Vector3 resolvedDirection = direction;
            Vector3 resolvedPosition = ResolveMovePosition(direction, out bool obstacleDetected);

            if (obstacleDetected)
            {
                RequestRepath();
            }

            Vector3 moved = resolvedPosition - transform.position;
            moved.y = 0f;
            if (moved.sqrMagnitude > 0.0001f)
            {
                resolvedDirection = moved.normalized;
            }

            Quaternion targetRotation = Quaternion.LookRotation(resolvedDirection, Vector3.up);
            Quaternion resolvedRotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeedDegrees * Time.fixedDeltaTime);

            if (body != null)
            {
                body.MovePosition(resolvedPosition);
                body.MoveRotation(resolvedRotation);
            }
            else
            {
                transform.SetPositionAndRotation(resolvedPosition, resolvedRotation);
            }
        }

        private void HandleMovementTargetChanged(Vector3 worldPosition)
        {
            movementTarget = worldPosition;
            hasMovementTarget = true;
        }

        private void HandleMovementTargetCleared()
        {
            hasMovementTarget = false;
        }

        private Vector3 ResolveMovePosition(Vector3 direction, out bool obstacleDetected)
        {
            obstacleDetected = false;

            Vector3 currentPosition = transform.position;
            float stepDistance = moveSpeed * Time.fixedDeltaTime;

            if (!useCollision || capsule == null)
            {
                return currentPosition + direction * stepDistance;
            }

            Vector3 directPosition = ResolveCandidateMove(direction, stepDistance);
            if (IsUsefulMove(currentPosition, directPosition, direction))
            {
                return directPosition;
            }

            obstacleDetected = true;

            for (var i = 0; i < AvoidanceAngles.Length; i++)
            {
                Vector3 avoidanceDirection = Quaternion.AngleAxis(AvoidanceAngles[i], Vector3.up) * direction;
                avoidanceDirection.y = 0f;

                if (avoidanceDirection.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                avoidanceDirection.Normalize();
                Vector3 candidatePosition = ResolveCandidateMove(avoidanceDirection, stepDistance);
                if (IsUsefulMove(currentPosition, candidatePosition, direction))
                {
                    return candidatePosition;
                }
            }

            return currentPosition;
        }

        private Vector3 ResolveCandidateMove(Vector3 direction, float stepDistance)
        {
            Vector3 desiredPosition = transform.position + direction * stepDistance;
            return PhysicsMovementUtility.ResolveCapsuleMove(
                transform,
                capsule,
                desiredPosition,
                collisionMask,
                collisionSkinWidth);
        }

        private bool IsUsefulMove(Vector3 from, Vector3 to, Vector3 desiredDirection)
        {
            Vector3 moved = to - from;
            moved.y = 0f;

            if (moved.sqrMagnitude < minimumMoveDistance * minimumMoveDistance)
            {
                return false;
            }

            return Vector3.Dot(moved.normalized, desiredDirection) > -0.15f;
        }

        private void RequestRepath()
        {
            if (botController == null || Time.time < nextRepathRequestTime)
            {
                return;
            }

            nextRepathRequestTime = Time.time + repathCooldown;
            botController.RequestRepath();
        }

        private void ResolvePhysicsComponents()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (body != null)
            {
                body.useGravity = false;
                body.isKinematic = true;
                body.detectCollisions = true;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            if (capsule == null)
            {
                capsule = GetComponent<CapsuleCollider>();
            }

            if (capsule != null)
            {
                capsule.enabled = true;
                capsule.isTrigger = false;
            }
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0.01f, moveSpeed);
            turnSpeedDegrees = Mathf.Max(1f, turnSpeedDegrees);
            stopDistance = Mathf.Max(0.001f, stopDistance);
            collisionSkinWidth = Mathf.Max(0f, collisionSkinWidth);
            minimumMoveDistance = Mathf.Max(0.001f, minimumMoveDistance);
            repathCooldown = Mathf.Max(0.05f, repathCooldown);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }
    }
}
#endif
