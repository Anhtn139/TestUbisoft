using UnityEngine;

namespace TestUbisoft.Prototype.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CameraFollowTarget : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 7f, -8f);
        [SerializeField] private bool lookAtTarget = true;
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 0.75f, 0f);
        [SerializeField, Min(0.05f)] private float targetSearchInterval = 0.25f;
        [SerializeField] private bool usePlayerFollowPosition = true;
        [SerializeField] private bool snapWhenTargetResolved = true;

        private float nextTargetSearchTime;
        private PlayerView targetPlayer;

        private void LateUpdate()
        {
            if (target == null && Time.time >= nextTargetSearchTime)
            {
                ResolveTarget();
                nextTargetSearchTime = Time.time + targetSearchInterval;
            }

            if (target == null)
            {
                return;
            }

            Vector3 targetPosition = GetTargetPosition();
            transform.position = targetPosition + offset;

            if (!lookAtTarget)
            {
                return;
            }

            Vector3 lookDirection = (targetPosition + lookAtOffset) - transform.position;
            if (lookDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            transform.rotation = desiredRotation;
        }

        private void ResolveTarget()
        {
            PlayerView[] players = FindObjectsOfType<PlayerView>();
            if (players == null || players.Length == 0)
            {
                return;
            }

            PlayerView selected = players[0];
            for (var i = 1; i < players.Length; i++)
            {
                if (players[i].EntityId < selected.EntityId)
                {
                    selected = players[i];
                }
            }

            targetPlayer = selected;
            target = selected.transform;

            if (snapWhenTargetResolved)
            {
                transform.position = GetTargetPosition() + offset;
            }
        }

        private Vector3 GetTargetPosition()
        {
            if (usePlayerFollowPosition && targetPlayer != null)
            {
                return targetPlayer.FollowPosition;
            }

            return target != null ? target.position : transform.position;
        }
    }
}
