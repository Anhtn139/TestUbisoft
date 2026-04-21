using System.Collections.Generic;
using TestUbisoft.Prototype.Core;
using TestUbisoft.Prototype.Messaging;

namespace TestUbisoft.Prototype.Server
{
    /// <summary>
    /// Authoritative simulation contract.
    /// Client presentation depends on snapshots/messages, not this concrete simulator.
    /// </summary>
    public interface IServerSimulator
    {
        void Initialize(GameConfig config, IMessageTransport transport);
        void Tick(float deltaSeconds, double serverTime);
        IReadOnlyList<BotDebugPathSnapshot> GetBotDebugPaths();
        void Shutdown();
    }
}
