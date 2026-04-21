using UnityEngine;

namespace TestUbisoft.Networking.Snapshots
{
    public enum SnapshotSampleState
    {
        None,
        Interpolated,
        Held,
        Extrapolated
    }

    public readonly struct SnapshotSample
    {
        private SnapshotSample(
            bool hasPose,
            double sampleTime,
            Vector3 position,
            Quaternion rotation,
            SnapshotSampleState state)
        {
            HasPose = hasPose;
            SampleTime = sampleTime;
            Position = position;
            Rotation = rotation;
            State = state;
        }

        public bool HasPose { get; }
        public double SampleTime { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public SnapshotSampleState State { get; }

        public static SnapshotSample Empty(double sampleTime)
        {
            return new SnapshotSample(false, sampleTime, Vector3.zero, Quaternion.identity, SnapshotSampleState.None);
        }

        public static SnapshotSample FromPose(
            double sampleTime,
            Vector3 position,
            Quaternion rotation,
            SnapshotSampleState state)
        {
            return new SnapshotSample(true, sampleTime, position, rotation, state);
        }
    }
}
