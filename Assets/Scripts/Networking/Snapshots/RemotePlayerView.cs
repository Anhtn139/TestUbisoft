using UnityEngine;

namespace TestUbisoft.Networking.Snapshots
{
    public sealed class RemotePlayerView : MonoBehaviour
    {
        [SerializeField]
        private string playerId;

        [SerializeField]
        private Transform poseTarget;

        private RemotePlayerSnapshotClient snapshotClient;

        public string PlayerId
        {
            get => playerId;
            set => playerId = value;
        }

        public SnapshotSampleState LastSampleState { get; private set; }

        private Transform PoseTarget => poseTarget != null ? poseTarget : transform;

        private void OnEnable()
        {
            snapshotClient = FindObjectOfType<RemotePlayerSnapshotClient>();
            snapshotClient?.RegisterView(this);
        }

        private void OnDisable()
        {
            snapshotClient?.UnregisterView(this);
            snapshotClient = null;
        }

        public void ApplySnapshotSample(SnapshotSample sample)
        {
            if (!sample.HasPose)
            {
                return;
            }

            LastSampleState = sample.State;
            PoseTarget.SetPositionAndRotation(sample.Position, sample.Rotation);
        }
    }
}
