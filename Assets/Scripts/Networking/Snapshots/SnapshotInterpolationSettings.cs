using System;
using UnityEngine;

namespace TestUbisoft.Networking.Snapshots
{
    [Serializable]
    public sealed class SnapshotInterpolationSettings
    {
        [SerializeField, Min(0f)]
        private float interpolationBackTimeSeconds = 0.1f;

        [SerializeField, Min(0f)]
        private float maxExtrapolationTimeSeconds = 0.05f;

        [SerializeField, Min(0f)]
        private float maxExtrapolationDistance = 0.35f;

        [SerializeField, Min(2)]
        private int maxBufferedSnapshotsPerPlayer = 32;

        public float InterpolationBackTimeSeconds
        {
            get => Mathf.Max(0f, interpolationBackTimeSeconds);
            set => interpolationBackTimeSeconds = Mathf.Max(0f, value);
        }

        public float MaxExtrapolationTimeSeconds
        {
            get => Mathf.Max(0f, maxExtrapolationTimeSeconds);
            set => maxExtrapolationTimeSeconds = Mathf.Max(0f, value);
        }

        public float MaxExtrapolationDistance
        {
            get => Mathf.Max(0f, maxExtrapolationDistance);
            set => maxExtrapolationDistance = Mathf.Max(0f, value);
        }

        public int MaxBufferedSnapshotsPerPlayer
        {
            get => Mathf.Max(2, maxBufferedSnapshotsPerPlayer);
            set => maxBufferedSnapshotsPerPlayer = Mathf.Max(2, value);
        }
    }
}
