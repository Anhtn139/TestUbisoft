namespace TestUbisoft.Prototype.Messaging
{
    /// <summary>
    /// Boundary between client and server.
    /// A real network implementation can replace the in-memory simulated transport without changing gameplay code.
    /// </summary>
    public interface IMessageTransport
    {
        void ConfigureLatency(float clientToServerMin, float clientToServerMax, float serverToClientMin, float serverToClientMax);
        void AdvanceTime(double timeSeconds);
        void SendToServer(ClientInputMessage message);
        void SendToClient(ServerSnapshotMessage message);
        bool TryDequeueForServer(out ClientInputMessage message);
        bool TryDequeueForClient(out ServerSnapshotMessage message);
        void Clear();
    }
}
