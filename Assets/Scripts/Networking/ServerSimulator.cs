using UnityEngine;

namespace TestUbisoft.Networking
{
    public sealed class ServerSimulator : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour transportComponent;
        [SerializeField] private Vector2 snapshotIntervalRange = new Vector2(0.1f, 0.5f);
        [SerializeField] private float simulatedMoveSpeed = 4f;
        [SerializeField] private string simulatedPlayerId = "remote-player";

        private IMessageTransport transport;
        private float nextSnapshotTime;
        private int nextSnapshotSequence;
        private Vector2 lastClientMoveInput;
        private Vector3 simulatedPlayerPosition;
        private Quaternion simulatedPlayerRotation = Quaternion.identity;

        private void Awake()
        {
            if (transportComponent != null)
            {
                transport = transportComponent as IMessageTransport;

                if (transport == null)
                {
                    Debug.LogError($"{nameof(ServerSimulator)} transport component must implement {nameof(IMessageTransport)}.", this);
                }
            }

            if (transport == null)
            {
                transport = FindObjectOfType<SimulatedMessageTransport>();
            }
        }

        private void OnEnable()
        {
            if (transport != null)
            {
                transport.MessageReceived += OnMessageReceived;
            }
        }

        private void Start()
        {
            ScheduleNextSnapshot(Time.time);
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
            if (transport == null)
            {
                return;
            }

            Vector3 moveDirection = new Vector3(lastClientMoveInput.x, 0f, lastClientMoveInput.y);
            simulatedPlayerPosition += moveDirection * (simulatedMoveSpeed * Time.deltaTime);

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                simulatedPlayerRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
            }

            if (Time.time >= nextSnapshotTime)
            {
                SendSnapshot(Time.time);
                ScheduleNextSnapshot(Time.time);
            }
        }

        private void OnMessageReceived(MessageEnvelope envelope)
        {
            if (envelope.Receiver != MessageEndpoint.Server)
            {
                return;
            }

            if (envelope.TryGetPayload(out ClientInputMessage inputMessage))
            {
                lastClientMoveInput = Vector2.ClampMagnitude(inputMessage.MoveInput, 1f);
            }
        }

        private void SendSnapshot(float currentTime)
        {
            ServerSnapshotMessage snapshot = new ServerSnapshotMessage
            {
                Sequence = nextSnapshotSequence++,
                PlayerId = simulatedPlayerId,
                PlayerPosition = simulatedPlayerPosition,
                PlayerRotation = simulatedPlayerRotation,
                ServerTime = currentTime
            };

            transport.Send(MessageEndpoint.Server, MessageEndpoint.Client, snapshot, currentTime);
        }

        private void ScheduleNextSnapshot(float currentTime)
        {
            float minInterval = Mathf.Clamp(Mathf.Min(snapshotIntervalRange.x, snapshotIntervalRange.y), 0.1f, 0.5f);
            float maxInterval = Mathf.Clamp(Mathf.Max(snapshotIntervalRange.x, snapshotIntervalRange.y), 0.1f, 0.5f);
            maxInterval = Mathf.Max(minInterval, maxInterval);

            nextSnapshotTime = currentTime + Random.Range(minInterval, maxInterval);
        }
    }
}
