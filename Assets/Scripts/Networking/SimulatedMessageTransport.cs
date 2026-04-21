using System.Collections.Generic;
using UnityEngine;

namespace TestUbisoft.Networking
{
    public sealed class SimulatedMessageTransport : MonoBehaviour, IMessageTransport
    {
        [SerializeField, Min(0f)] private float latencySeconds = 0.1f;
        [SerializeField, Min(0f)] private float jitterSeconds = 0.05f;
        [SerializeField] private bool autoTick = true;
        [SerializeField] private bool debugLogging = true;

        private readonly List<MessageEnvelope> pendingMessages = new List<MessageEnvelope>();
        private int nextMessageId = 1;

        public event System.Action<MessageEnvelope> MessageReceived;

        public float LatencySeconds
        {
            get => latencySeconds;
            set => latencySeconds = Mathf.Max(0f, value);
        }

        public float JitterSeconds
        {
            get => jitterSeconds;
            set => jitterSeconds = Mathf.Max(0f, value);
        }

        public int PendingMessageCount => pendingMessages.Count;

        private void Update()
        {
            if (autoTick)
            {
                Tick(Time.time);
            }
        }

        public void Send(MessageEndpoint sender, MessageEndpoint receiver, object payload, float currentTime)
        {
            float deliveryDelay = CalculateDeliveryDelay();
            float scheduledDeliveryAt = currentTime + deliveryDelay;
            MessageEnvelope envelope = new MessageEnvelope(
                nextMessageId++,
                sender,
                receiver,
                payload,
                currentTime,
                scheduledDeliveryAt);

            Enqueue(envelope);

            if (debugLogging)
            {
                Debug.Log(
                    $"[Transport] Sent {envelope.PayloadType} #{envelope.MessageId} " +
                    $"{sender}->{receiver} at {currentTime:F3}s, scheduled for {scheduledDeliveryAt:F3}s " +
                    $"(delay {deliveryDelay:F3}s).");
            }
        }

        public void Tick(float currentTime)
        {
            while (pendingMessages.Count > 0 && pendingMessages[0].ScheduledDeliveryAt <= currentTime)
            {
                MessageEnvelope envelope = pendingMessages[0];
                pendingMessages.RemoveAt(0);
                envelope.MarkReceived(currentTime);

                if (debugLogging)
                {
                    Debug.Log(
                        $"[Transport] Received {envelope.PayloadType} #{envelope.MessageId} " +
                        $"{envelope.Sender}->{envelope.Receiver} at {currentTime:F3}s " +
                        $"(sent {envelope.SentAt:F3}s, scheduled {envelope.ScheduledDeliveryAt:F3}s).");
                }

                MessageReceived?.Invoke(envelope);
            }
        }

        private float CalculateDeliveryDelay()
        {
            float jitterOffset = jitterSeconds > 0f ? Random.Range(-jitterSeconds, jitterSeconds) : 0f;
            return Mathf.Max(0f, latencySeconds + jitterOffset);
        }

        private void Enqueue(MessageEnvelope envelope)
        {
            int insertIndex = pendingMessages.Count;

            for (int index = 0; index < pendingMessages.Count; index++)
            {
                if (envelope.ScheduledDeliveryAt < pendingMessages[index].ScheduledDeliveryAt)
                {
                    insertIndex = index;
                    break;
                }
            }

            pendingMessages.Insert(insertIndex, envelope);
        }
    }
}
