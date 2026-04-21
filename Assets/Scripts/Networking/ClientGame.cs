using UnityEngine;
using TestUbisoft.Networking.Snapshots;

namespace TestUbisoft.Networking
{
    public sealed class ClientGame : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour transportComponent;
        [SerializeField, Min(0.01f)] private float inputSendIntervalSeconds = 0.05f;
        [SerializeField] private Transform playerView;
        [SerializeField] private string remotePlayerId = "remote-player";
        [SerializeField] private SnapshotInterpolationSettings interpolationSettings = new SnapshotInterpolationSettings();
        [SerializeField] private float snapshotClockOffsetSeconds;

        private IMessageTransport transport;
        private RemotePlayerSnapshotInterpolator snapshotInterpolator;
        private float nextInputSendTime;
        private int nextInputSequence;

        private void Awake()
        {
            if (transportComponent != null)
            {
                transport = transportComponent as IMessageTransport;

                if (transport == null)
                {
                    Debug.LogError($"{nameof(ClientGame)} transport component must implement {nameof(IMessageTransport)}.", this);
                }
            }

            if (transport == null)
            {
                transport = FindObjectOfType<SimulatedMessageTransport>();
            }

            if (interpolationSettings == null)
            {
                interpolationSettings = new SnapshotInterpolationSettings();
            }

            snapshotInterpolator = new RemotePlayerSnapshotInterpolator(interpolationSettings);
        }

        private void OnEnable()
        {
            if (transport != null)
            {
                transport.MessageReceived += OnMessageReceived;
            }
        }

        private void OnDisable()
        {
            if (transport != null)
            {
                transport.MessageReceived -= OnMessageReceived;
            }
        }

        private void Update()
        {
            RenderRemotePlayer(Time.timeAsDouble + snapshotClockOffsetSeconds);

            if (transport == null || Time.time < nextInputSendTime)
            {
                return;
            }

            SendInput(Time.time);
            nextInputSendTime = Time.time + inputSendIntervalSeconds;
        }

        private void OnMessageReceived(MessageEnvelope envelope)
        {
            if (envelope.Receiver != MessageEndpoint.Client)
            {
                return;
            }

            if (envelope.TryGetPayload(out ServerSnapshotMessage snapshot))
            {
                string playerId = string.IsNullOrWhiteSpace(snapshot.PlayerId)
                    ? remotePlayerId
                    : snapshot.PlayerId;

                snapshotInterpolator.ReceiveSnapshot(
                    playerId,
                    new PlayerSnapshot(snapshot.ServerTime, snapshot.PlayerPosition, snapshot.PlayerRotation));
            }
        }

        private void RenderRemotePlayer(double snapshotClockTime)
        {
            if (playerView == null)
            {
                return;
            }

            if (snapshotInterpolator.TrySample(remotePlayerId, snapshotClockTime, out SnapshotSample sample)
                && sample.HasPose)
            {
                playerView.SetPositionAndRotation(sample.Position, sample.Rotation);
            }
        }

        private void SendInput(float currentTime)
        {
            ClientInputMessage inputMessage = new ClientInputMessage
            {
                Sequence = nextInputSequence++,
                MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                FirePressed = Input.GetButton("Fire1"),
                ClientTime = currentTime
            };

            transport.Send(MessageEndpoint.Client, MessageEndpoint.Server, inputMessage, currentTime);
        }
    }
}
