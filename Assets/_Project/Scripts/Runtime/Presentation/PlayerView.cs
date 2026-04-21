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

        [SerializeField] private LayerMask localCollisionMask = ~0;
        [SerializeField, Min(0f)] private float localCollisionSkinWidth = 0.03f;

        private Rigidbody body;
        private CapsuleCollider capsule;
        private bool usePhysicalCollision;
        private bool hasResolvedPhysicsComponents;

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
            SetPose(position, rotation);
        }

        private void SetPose(Vector3 position, Quaternion rotation)
        {
            if (usePhysicalCollision && capsule != null)
            {
                position = PhysicsMovementUtility.ResolveCapsuleMove(
                    transform,
                    capsule,
                    position,
                    localCollisionMask,
                    localCollisionSkinWidth);
            }

            transform.SetPositionAndRotation(position, rotation);
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
                capsule.isTrigger = !usePhysicalCollision;
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
                childColliders[i].isTrigger = childColliders[i] != capsule || !usePhysicalCollision;
            }

            hasResolvedPhysicsComponents = true;
        }
    }
}
