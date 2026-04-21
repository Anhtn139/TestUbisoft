using System;

namespace TestUbisoft.Networking
{
    public interface IMessageTransport
    {
        event Action<MessageEnvelope> MessageReceived;

        void Send(MessageEndpoint sender, MessageEndpoint receiver, object payload, float currentTime);
        void Tick(float currentTime);
    }
}
