using System.Collections.Generic;
using UnityEngine;

namespace TestUbisoft.Networking.Snapshots
{
    public sealed class RemotePlayerSnapshotClient : MonoBehaviour
    {
        [SerializeField]
        private SnapshotInterpolationSettings interpolationSettings = new SnapshotInterpolationSettings();

        [SerializeField]
        private float snapshotClockOffsetSeconds;

        [SerializeField]
        private bool autoRegisterSceneViews = true;

        private readonly List<RemotePlayerView> views = new List<RemotePlayerView>();
        private RemotePlayerSnapshotInterpolator interpolator;

        public SnapshotInterpolationSettings InterpolationSettings => interpolationSettings;

        public double SnapshotClockTime => Time.timeAsDouble + snapshotClockOffsetSeconds;

        private void Awake()
        {
            if (interpolationSettings == null)
            {
                interpolationSettings = new SnapshotInterpolationSettings();
            }

            interpolator = new RemotePlayerSnapshotInterpolator(interpolationSettings);
        }

        private void Start()
        {
            if (!autoRegisterSceneViews)
            {
                return;
            }

            RemotePlayerView[] sceneViews = FindObjectsOfType<RemotePlayerView>();
            for (int i = 0; i < sceneViews.Length; i++)
            {
                RegisterView(sceneViews[i]);
            }
        }

        private void Update()
        {
            double snapshotTime = SnapshotClockTime;

            for (int i = views.Count - 1; i >= 0; i--)
            {
                RemotePlayerView view = views[i];
                if (view == null)
                {
                    views.RemoveAt(i);
                    continue;
                }

                if (interpolator.TrySample(view.PlayerId, snapshotTime, out SnapshotSample sample))
                {
                    view.ApplySnapshotSample(sample);
                }
            }
        }

        public void RegisterView(RemotePlayerView view)
        {
            if (view != null && !views.Contains(view))
            {
                views.Add(view);
            }
        }

        public void UnregisterView(RemotePlayerView view)
        {
            views.Remove(view);
        }

        public void ReceiveSnapshot(string playerId, double timestamp, Vector3 position, Quaternion rotation)
        {
            interpolator.ReceiveSnapshot(playerId, new PlayerSnapshot(timestamp, position, rotation));
        }

        public void SetSnapshotClockOffset(double offsetSeconds)
        {
            snapshotClockOffsetSeconds = (float)offsetSeconds;
        }

        public void RemovePlayer(string playerId)
        {
            interpolator.RemovePlayer(playerId);
        }
    }
}
