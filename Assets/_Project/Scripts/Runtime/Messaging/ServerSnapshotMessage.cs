using TestUbisoft.Prototype.Core;

namespace TestUbisoft.Prototype.Messaging
{
    /// <summary>
    /// Server-to-client state payload. Presentation layers should render this without embedding gameplay rules.
    /// </summary>
    public readonly struct ServerSnapshotMessage
    {
        public readonly WorldSnapshot Snapshot;

        public ServerSnapshotMessage(WorldSnapshot snapshot)
        {
            Snapshot = snapshot;
        }
    }
}
