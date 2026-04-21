using TestUbisoft.Prototype.Core;
using TestUbisoft.Prototype.Messaging;

namespace TestUbisoft.Prototype.Client
{
    /// <summary>
    /// Client-side runtime contract. It sends player intent and consumes server snapshots.
    /// It does not own authoritative gameplay decisions.
    /// </summary>
    public interface IClientGame
    {
        void Initialize(GameConfig config, IMessageTransport transport, IClientWorldView worldView);
        void SetLocalMoveInput(SimVector2 moveInput);
        void Tick(float deltaSeconds, double clientTime);
        void Shutdown();
    }
}
