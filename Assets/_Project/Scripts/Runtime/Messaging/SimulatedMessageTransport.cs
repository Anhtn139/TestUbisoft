using System.Collections.Generic;
using UnityEngine;

namespace TestUbisoft.Prototype.Messaging
{
    /// <summary>
    /// In-process message queues used by the prototype.
    /// It intentionally behaves like a transport boundary even though both sides run in the same Unity player.
    /// </summary>
    public sealed class SimulatedMessageTransport : IMessageTransport
    {
        private readonly List<ScheduledMessage<ClientInputMessage>> _serverInbox = new List<ScheduledMessage<ClientInputMessage>>();
        private readonly List<ScheduledMessage<ServerSnapshotMessage>> _clientInbox = new List<ScheduledMessage<ServerSnapshotMessage>>();

        private double _currentTime;
        private Vector2 _clientToServerLatency = new Vector2(0.03f, 0.12f);
        private Vector2 _serverToClientLatency = new Vector2(0.08f, 0.25f);

        public void ConfigureLatency(
            float clientToServerMin,
            float clientToServerMax,
            float serverToClientMin,
            float serverToClientMax)
        {
            _clientToServerLatency = NormalizeRange(clientToServerMin, clientToServerMax);
            _serverToClientLatency = NormalizeRange(serverToClientMin, serverToClientMax);
        }

        public void AdvanceTime(double timeSeconds)
        {
            _currentTime = timeSeconds;
        }

        public void SendToServer(ClientInputMessage message)
        {
            _serverInbox.Add(new ScheduledMessage<ClientInputMessage>(
                _currentTime + RandomLatency(_clientToServerLatency),
                message));
        }

        public void SendToClient(ServerSnapshotMessage message)
        {
            _clientInbox.Add(new ScheduledMessage<ServerSnapshotMessage>(
                _currentTime + RandomLatency(_serverToClientLatency),
                message));
        }

        public bool TryDequeueForServer(out ClientInputMessage message)
        {
            return TryDequeue(_serverInbox, out message);
        }

        public bool TryDequeueForClient(out ServerSnapshotMessage message)
        {
            return TryDequeue(_clientInbox, out message);
        }

        public void Clear()
        {
            _serverInbox.Clear();
            _clientInbox.Clear();
        }

        private bool TryDequeue<T>(List<ScheduledMessage<T>> inbox, out T message)
        {
            var readyIndex = -1;
            var readyTime = double.PositiveInfinity;
            for (var i = 0; i < inbox.Count; i++)
            {
                if (inbox[i].DeliveryTime > _currentTime || inbox[i].DeliveryTime >= readyTime)
                {
                    continue;
                }

                readyIndex = i;
                readyTime = inbox[i].DeliveryTime;
            }

            if (readyIndex < 0)
            {
                message = default;
                return false;
            }

            message = inbox[readyIndex].Message;
            inbox.RemoveAt(readyIndex);
            return true;
        }

        private static Vector2 NormalizeRange(float min, float max)
        {
            var normalizedMin = Mathf.Max(0f, Mathf.Min(min, max));
            var normalizedMax = Mathf.Max(normalizedMin, Mathf.Max(min, max));
            return new Vector2(normalizedMin, normalizedMax);
        }

        private static float RandomLatency(Vector2 range)
        {
            if (range.y <= range.x)
            {
                return range.x;
            }

            return Random.Range(range.x, range.y);
        }

        private readonly struct ScheduledMessage<T>
        {
            public readonly double DeliveryTime;
            public readonly T Message;

            public ScheduledMessage(double deliveryTime, T message)
            {
                DeliveryTime = deliveryTime;
                Message = message;
            }
        }
    }
}
