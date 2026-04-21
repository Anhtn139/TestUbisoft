using TestUbisoft.Prototype.Core;

namespace TestUbisoft.Prototype.Messaging
{
    /// <summary>
    /// Client-to-server command. The server treats this as intent, not truth.
    /// </summary>
    public readonly struct ClientInputMessage
    {
        public readonly string ClientId;
        public readonly int Sequence;
        public readonly SimVector2 MoveInput;

        public ClientInputMessage(string clientId, int sequence, SimVector2 moveInput)
        {
            ClientId = clientId;
            Sequence = sequence;
            MoveInput = moveInput;
        }
    }
}
