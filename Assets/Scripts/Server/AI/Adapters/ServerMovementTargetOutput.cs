#if UNITY_SERVER || UNITY_EDITOR
using System;
using UnityEngine;

namespace TestUbisoft.Server.AI
{
    [DisallowMultipleComponent]
    public sealed class ServerMovementTargetOutput : MonoBehaviour, IServerMovementTargetSink
    {
        public event Action<Vector3> MovementTargetChanged;
        public event Action MovementTargetCleared;

        public bool HasMovementTarget { get; private set; }

        public Vector3 MovementTarget { get; private set; }

        public void SetMovementTarget(Vector3 worldPosition)
        {
            HasMovementTarget = true;
            MovementTarget = worldPosition;
            MovementTargetChanged?.Invoke(worldPosition);
        }

        public void ClearMovementTarget()
        {
            if (!HasMovementTarget)
            {
                return;
            }

            HasMovementTarget = false;
            MovementTargetCleared?.Invoke();
        }
    }
}
#endif
