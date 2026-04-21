using UnityEngine;

namespace TestUbisoft.Prototype.Presentation
{
    public static class PhysicsMovementUtility
    {
        private const int MaxSlideIterations = 3;

        public static Vector3 ResolveCapsuleMove(
            Transform mover,
            CapsuleCollider capsule,
            Vector3 desiredPosition,
            LayerMask collisionMask,
            float skinWidth)
        {
            Vector3 currentPosition = mover.position;
            if (capsule == null)
            {
                return desiredPosition;
            }

            Vector3 displacement = desiredPosition - currentPosition;
            float distance = displacement.magnitude;
            if (distance <= 0.0001f)
            {
                return desiredPosition;
            }

            Vector3 resolvedPosition = currentPosition;
            Vector3 remaining = displacement;

            for (var iteration = 0; iteration < MaxSlideIterations; iteration++)
            {
                float remainingDistance = remaining.magnitude;
                if (remainingDistance <= 0.0001f)
                {
                    break;
                }

                Vector3 direction = remaining / remainingDistance;
                if (!TryCapsuleCast(
                        mover,
                        capsule,
                        resolvedPosition,
                        direction,
                        remainingDistance,
                        collisionMask,
                        out RaycastHit hit))
                {
                    resolvedPosition += remaining;
                    break;
                }

                float allowedDistance = Mathf.Max(0f, hit.distance - skinWidth);
                if (allowedDistance > 0.0001f)
                {
                    resolvedPosition += direction * Mathf.Min(remainingDistance, allowedDistance);
                }

                Vector3 remainingAfterHit = desiredPosition - resolvedPosition;
                remainingAfterHit.y = 0f;

                Vector3 slideNormal = hit.normal;
                slideNormal.y = 0f;
                if (slideNormal.sqrMagnitude <= 0.0001f)
                {
                    break;
                }

                slideNormal.Normalize();
                remaining = Vector3.ProjectOnPlane(remainingAfterHit, slideNormal);
                remaining.y = 0f;

                if (Vector3.Dot(remaining, direction) <= 0.0001f)
                {
                    break;
                }
            }

            return resolvedPosition;
        }

        private static bool TryCapsuleCast(
            Transform mover,
            CapsuleCollider capsule,
            Vector3 currentPosition,
            Vector3 direction,
            float distance,
            LayerMask collisionMask,
            out RaycastHit nearestHit)
        {
            nearestHit = default;
            GetCapsuleWorldGeometry(mover, capsule, currentPosition, out Vector3 pointA, out Vector3 pointB, out float radius);

            RaycastHit[] hits = Physics.CapsuleCastAll(
                pointA,
                pointB,
                radius,
                direction,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = float.PositiveInfinity;
            bool hasHit = false;
            for (var i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null || hitCollider.transform == mover || hitCollider.transform.IsChildOf(mover))
                {
                    continue;
                }

                if (hits[i].normal.y > 0.65f)
                {
                    continue;
                }

                if (hits[i].distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hits[i].distance;
                nearestHit = hits[i];
                hasHit = true;
            }

            return hasHit;
        }

        private static void GetCapsuleWorldGeometry(
            Transform mover,
            CapsuleCollider capsule,
            Vector3 rootPosition,
            out Vector3 pointA,
            out Vector3 pointB,
            out float radius)
        {
            Transform capsuleTransform = capsule.transform;
            Vector3 scale = capsuleTransform.lossyScale;
            Vector3 axis;
            float axisScale;
            float radiusScale;

            switch (capsule.direction)
            {
                case 0:
                    axis = capsuleTransform.right;
                    axisScale = Mathf.Abs(scale.x);
                    radiusScale = Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                    break;
                case 2:
                    axis = capsuleTransform.forward;
                    axisScale = Mathf.Abs(scale.z);
                    radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                    break;
                default:
                    axis = capsuleTransform.up;
                    axisScale = Mathf.Abs(scale.y);
                    radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                    break;
            }

            radius = Mathf.Max(0.001f, capsule.radius * radiusScale);
            float height = Mathf.Max(radius * 2f, capsule.height * axisScale);
            float halfSegment = Mathf.Max(0f, (height * 0.5f) - radius);
            Vector3 center = GetCapsuleWorldCenter(mover, capsule, rootPosition);

            pointA = center + axis * halfSegment;
            pointB = center - axis * halfSegment;
        }

        public static Vector3 GetCapsuleWorldCenter(Transform mover, CapsuleCollider capsule, Vector3 rootPosition)
        {
            Transform capsuleTransform = capsule.transform;
            Vector3 centerOffset = capsuleTransform.TransformPoint(capsule.center) - mover.position;
            return rootPosition + centerOffset;
        }
    }
}
