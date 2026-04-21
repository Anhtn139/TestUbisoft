using UnityEngine;

namespace TestUbisoft.Networking.Snapshots
{
    public readonly struct PlayerSnapshot
    {
        public PlayerSnapshot(double timestamp, Vector3 position, Quaternion rotation)
        {
            Timestamp = timestamp;
            Position = position;
            Rotation = rotation;
        }

        public double Timestamp { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
    }
}
