#if UNITY_SERVER || UNITY_EDITOR
using UnityEngine;

namespace TestUbisoft.Server.AI
{
    public abstract class EggTargetSelectionStrategy : ScriptableObject, IEggTargetSelectionStrategy
    {
        public abstract bool TrySelectTarget(BotTargetSelectionContext context, out EggTargetSelection selection);
    }
}
#endif
