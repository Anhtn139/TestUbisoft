using UnityEngine;

namespace TestUbisoft.Prototype.Presentation
{
    /// <summary>
    /// Visual-only player representation.
    /// It applies already-authoritative snapshot data and contains no gameplay decision-making.
    /// </summary>
    public sealed class PlayerView : MonoBehaviour
    {
        private const int ActorLayer = 7;
        private const float RemoteSnapDistance = 1.5f;

        [SerializeField] private LayerMask localCollisionMask = ~0;
        [SerializeField, Min(0f)] private float localCollisionSkinWidth = 0.03f;
        [SerializeField, Min(0.005f)] private float remotePositionSmoothTime = 0.045f;
        [SerializeField, Min(90f)] private float remoteRotationSpeed = 900f;

        private Rigidbody body;
        private CapsuleCollider capsule;
        private bool usePhysicalCollision;
        private bool hasResolvedPhysicsComponents;
        private bool hasTargetPose;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private Vector3 smoothVelocity;

        public int EntityId { get; private set; }

        public Vector3 FollowPosition
        {
            get
            {
                ResolvePhysicsComponents();
                return capsule != null
                    ? PhysicsMovementUtility.GetCapsuleWorldCenter(transform, capsule, transform.position)
                    : transform.position + Vector3.up;
            }
        }

        public void Initialize(int entityId, bool enablePhysicalCollision)
        {
            EntityId = entityId;
            usePhysicalCollision = enablePhysicalCollision;
            name = $"PlayerView_{entityId}";
            SetLayerRecursively(transform, ActorLayer);
            ResolvePhysicsComponents();
        }

        public void ApplySnapshot(Vector3 position, Quaternion rotation)
        {
            ResolvePhysicsComponents();
            targetPosition = position;
            targetRotation = rotation;

            if (!hasTargetPose || usePhysicalCollision)
            {
                smoothVelocity = Vector3.zero;
                SetPose(position, rotation);
            }

            hasTargetPose = true;
        }

        private void LateUpdate()
        {
            if (!hasTargetPose || usePhysicalCollision)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 currentPosition = transform.position;
            if ((targetPosition - currentPosition).sqrMagnitude >= RemoteSnapDistance * RemoteSnapDistance)
            {
                smoothVelocity = Vector3.zero;
                transform.SetPositionAndRotation(targetPosition, targetRotation);
                return;
            }

            Vector3 smoothedPosition = Vector3.SmoothDamp(
                currentPosition,
                targetPosition,
                ref smoothVelocity,
                remotePositionSmoothTime,
                Mathf.Infinity,
                deltaTime);

            Quaternion smoothedRotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                remoteRotationSpeed * deltaTime);

            transform.SetPositionAndRotation(smoothedPosition, smoothedRotation);
        }

        private void SetPose(Vector3 position, Quaternion rotation)
        {
            if (usePhysicalCollision && capsule != null)
            {
                position = PhysicsMovementUtility.ResolveCapsuleMove(
                    transform,
                    capsule,
                    position,
                    GetEffectiveLocalCollisionMask(),
                    localCollisionSkinWidth);
            }

            transform.SetPositionAndRotation(position, rotation);
        }

        private LayerMask GetEffectiveLocalCollisionMask()
        {
            return usePhysicalCollision
                ? localCollisionMask | (1 << ActorLayer)
                : localCollisionMask;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        private void ResolvePhysicsComponents()
        {
            if (hasResolvedPhysicsComponents)
            {
                return;
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (body != null)
            {
                body.useGravity = false;
                body.isKinematic = true;
                body.detectCollisions = true;
                body.interpolation = usePhysicalCollision
                    ? RigidbodyInterpolation.Interpolate
                    : RigidbodyInterpolation.None;
                body.collisionDetectionMode = usePhysicalCollision
                    ? CollisionDetectionMode.ContinuousSpeculative
                    : CollisionDetectionMode.Discrete;
            }

            capsule ??= GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = GetComponentInChildren<CapsuleCollider>();
            }

            if (capsule != null)
            {
                capsule.enabled = true;
                capsule.isTrigger = false;
            }

            Rigidbody[] childBodies = GetComponentsInChildren<Rigidbody>();
            for (var i = 0; i < childBodies.Length; i++)
            {
                childBodies[i].useGravity = false;
                childBodies[i].isKinematic = true;
                childBodies[i].detectCollisions = true;
                childBodies[i].interpolation = RigidbodyInterpolation.None;
                childBodies[i].collisionDetectionMode = CollisionDetectionMode.Discrete;
            }

            Collider[] childColliders = GetComponentsInChildren<Collider>();
            for (var i = 0; i < childColliders.Length; i++)
            {
                childColliders[i].enabled = true;
                childColliders[i].isTrigger = childColliders[i] != capsule;
            }

            hasResolvedPhysicsComponents = true;
        }
    }
}
