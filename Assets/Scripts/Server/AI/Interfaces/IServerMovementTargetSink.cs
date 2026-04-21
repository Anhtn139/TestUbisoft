#if UNITY_SERVER || UNITY_EDITOR
using UnityEngine;

namespace TestUbisoft.Server.AI
{
    public interface IServerMovementTargetSink
    {
        void SetMovementTarget(Vector3 worldPosition);

        void ClearMovementTarget();
    }
}
#endif
