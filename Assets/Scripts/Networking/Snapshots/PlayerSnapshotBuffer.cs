using System;
using System.Collections.Generic;
using UnityEngine;

namespace TestUbisoft.Networking.Snapshots
{
    public sealed class PlayerSnapshotBuffer
    {
        private readonly List<PlayerSnapshot> snapshots = new List<PlayerSnapshot>();
        private readonly SnapshotInterpolationSettings settings;

        public PlayerSnapshotBuffer(SnapshotInterpolationSettings settings)
        {
            this.settings = settings ?? new SnapshotInterpolationSettings();
        }

        public int Count => snapshots.Count;

        public void Clear()
        {
            snapshots.Clear();
        }

        public void AddSnapshot(PlayerSnapshot snapshot)
        {
            if (double.IsNaN(snapshot.Timestamp) || double.IsInfinity(snapshot.Timestamp))
            {
                return;
            }

            int insertIndex = FindFirstIndexAtOrAfter(snapshot.Timestamp);
            if (insertIndex < snapshots.Count && snapshots[insertIndex].Timestamp == snapshot.Timestamp)
            {
                snapshots[insertIndex] = snapshot;
            }
            else
            {
                snapshots.Insert(insertIndex, snapshot);
            }

            TrimToCapacity();
        }

        public bool TrySample(double snapshotClockTime, out SnapshotSample sample)
        {
            double renderTime = snapshotClockTime - settings.InterpolationBackTimeSeconds;

            if (snapshots.Count == 0)
            {
                sample = SnapshotSample.Empty(renderTime);
                return false;
            }

            if (snapshots.Count == 1)
            {
                PlayerSnapshot onlySnapshot = snapshots[0];
                sample = SnapshotSample.FromPose(
                    renderTime,
                    onlySnapshot.Position,
                    onlySnapshot.Rotation,
                    SnapshotSampleState.Held);
                return true;
            }

            int futureIndex = FindFirstIndexAtOrAfter(renderTime);

            if (futureIndex == 0)
            {
                PlayerSnapshot firstSnapshot = snapshots[0];
                sample = SnapshotSample.FromPose(
                    renderTime,
                    firstSnapshot.Position,
                    firstSnapshot.Rotation,
                    SnapshotSampleState.Held);
                return true;
            }

            if (futureIndex < snapshots.Count)
            {
                PlayerSnapshot previous = snapshots[futureIndex - 1];
                PlayerSnapshot future = snapshots[futureIndex];
                sample = Interpolate(renderTime, previous, future);
                PruneBefore(futureIndex - 1);
                return true;
            }

            sample = SampleAfterNewestSnapshot(renderTime);
            PruneBefore(Mathf.Max(0, snapshots.Count - 2));
            return true;
        }

        private SnapshotSample Interpolate(double renderTime, PlayerSnapshot previous, PlayerSnapshot future)
        {
            double duration = future.Timestamp - previous.Timestamp;
            float t = duration <= 0d
                ? 1f
                : Mathf.Clamp01((float)((renderTime - previous.Timestamp) / duration));

            Vector3 position = Vector3.LerpUnclamped(previous.Position, future.Position, t);
            Quaternion rotation = Quaternion.SlerpUnclamped(previous.Rotation, future.Rotation, t);

            return SnapshotSample.FromPose(renderTime, position, rotation, SnapshotSampleState.Interpolated);
        }

        private SnapshotSample SampleAfterNewestSnapshot(double renderTime)
        {
            PlayerSnapshot newest = snapshots[snapshots.Count - 1];
            PlayerSnapshot previous = snapshots[snapshots.Count - 2];
            double overshootSeconds = renderTime - newest.Timestamp;

            if (settings.MaxExtrapolationTimeSeconds <= 0f
                || overshootSeconds <= 0d
                || overshootSeconds > settings.MaxExtrapolationTimeSeconds
                || newest.Timestamp <= previous.Timestamp)
            {
                return SnapshotSample.FromPose(
                    renderTime,
                    newest.Position,
                    newest.Rotation,
                    SnapshotSampleState.Held);
            }

            double previousDelta = newest.Timestamp - previous.Timestamp;
            Vector3 velocity = (newest.Position - previous.Position) / (float)previousDelta;
            Vector3 displacement = velocity * (float)overshootSeconds;
            displacement = Vector3.ClampMagnitude(displacement, settings.MaxExtrapolationDistance);

            return SnapshotSample.FromPose(
                renderTime,
                newest.Position + displacement,
                newest.Rotation,
                SnapshotSampleState.Extrapolated);
        }

        private int FindFirstIndexAtOrAfter(double timestamp)
        {
            int low = 0;
            int high = snapshots.Count;

            while (low < high)
            {
                int middle = low + ((high - low) / 2);
                if (snapshots[middle].Timestamp < timestamp)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        private void PruneBefore(int firstIndexToKeep)
        {
            if (firstIndexToKeep <= 0)
            {
                return;
            }

            snapshots.RemoveRange(0, firstIndexToKeep);
        }

        private void TrimToCapacity()
        {
            int overflowCount = snapshots.Count - settings.MaxBufferedSnapshotsPerPlayer;
            if (overflowCount > 0)
            {
                snapshots.RemoveRange(0, overflowCount);
            }
        }
    }
}
