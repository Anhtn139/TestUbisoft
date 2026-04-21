using System.Collections.Generic;

namespace TestUbisoft.Networking.Snapshots
{
    public sealed class RemotePlayerSnapshotInterpolator
    {
        private readonly Dictionary<string, PlayerSnapshotBuffer> buffers =
            new Dictionary<string, PlayerSnapshotBuffer>();

        private readonly SnapshotInterpolationSettings settings;

        public RemotePlayerSnapshotInterpolator(SnapshotInterpolationSettings settings)
        {
            this.settings = settings ?? new SnapshotInterpolationSettings();
        }

        public int PlayerCount => buffers.Count;

        public void ReceiveSnapshot(string playerId, PlayerSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            GetOrCreateBuffer(playerId).AddSnapshot(snapshot);
        }

        public bool TrySample(string playerId, double snapshotClockTime, out SnapshotSample sample)
        {
            if (!string.IsNullOrWhiteSpace(playerId)
                && buffers.TryGetValue(playerId, out PlayerSnapshotBuffer buffer))
            {
                return buffer.TrySample(snapshotClockTime, out sample);
            }

            sample = SnapshotSample.Empty(snapshotClockTime - settings.InterpolationBackTimeSeconds);
            return false;
        }

        public void RemovePlayer(string playerId)
        {
            if (!string.IsNullOrWhiteSpace(playerId))
            {
                buffers.Remove(playerId);
            }
        }

        public void Clear()
        {
            buffers.Clear();
        }

        private PlayerSnapshotBuffer GetOrCreateBuffer(string playerId)
        {
            if (!buffers.TryGetValue(playerId, out PlayerSnapshotBuffer buffer))
            {
                buffer = new PlayerSnapshotBuffer(settings);
                buffers.Add(playerId, buffer);
            }

            return buffer;
        }
    }
}
