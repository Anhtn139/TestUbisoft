using TestUbisoft.Prototype.Core;

namespace TestUbisoft.Prototype.Client
{
    /// <summary>
    /// View boundary for the client runtime.
    /// Implementations may use GameObjects, ECS, pooling, or test doubles without changing client gameplay flow.
    /// </summary>
    public interface IClientWorldView
    {
        void Render(WorldSnapshot snapshot);
        void Clear();
    }
}
