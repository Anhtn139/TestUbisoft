#if UNITY_SERVER || UNITY_EDITOR
using System.Collections.Generic;

namespace TestUbisoft.Server.AI
{
    public interface IEggProvider
    {
        IReadOnlyList<EggTarget> GetAvailableEggs();

        bool TryGetEgg(string eggId, out EggTarget egg);

        bool IsEggValid(string eggId);
    }
}
#endif
