using System;
using UnityEngine;

namespace TestUbisoft.Networking
{
    [Serializable]
    public sealed class MessageEnvelope
    {
        public MessageEnvelope(
            int messageId,
            MessageEndpoint sender,
            MessageEndpoint receiver,
            object payload,
            float sentAt,
            float scheduledDeliveryAt)
        {
            MessageId = messageId;
            Sender = sender;
            Receiver = receiver;
            Payload = payload;
            PayloadType = payload != null ? payload.GetType().Name : "null";
            SentAt = sentAt;
            ScheduledDeliveryAt = scheduledDeliveryAt;
        }

        public int MessageId { get; }
        public MessageEndpoint Sender { get; }
        public MessageEndpoint Receiver { get; }
        public object Payload { get; }
        public string PayloadType { get; }
        public float SentAt { get; }
        public float ScheduledDeliveryAt { get; }
        public float? ReceivedAt { get; private set; }

        public bool TryGetPayload<TPayload>(out TPayload payload)
        {
            if (Payload is TPayload typedPayload)
            {
                payload = typedPayload;
                return true;
            }

            payload = default;
            return false;
        }

        internal void MarkReceived(float receivedAt)
        {
            ReceivedAt = receivedAt;
        }

        public override string ToString()
        {
            string receivedAtText = ReceivedAt.HasValue ? ReceivedAt.Value.ToString("F3") : "pending";

            return $"Message #{MessageId} {PayloadType} {Sender}->{Receiver} " +
                   $"sent={SentAt:F3}s scheduled={ScheduledDeliveryAt:F3}s received={receivedAtText}s";
        }
    }
}
