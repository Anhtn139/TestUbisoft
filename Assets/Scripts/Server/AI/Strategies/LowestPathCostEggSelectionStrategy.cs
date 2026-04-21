#if UNITY_SERVER || UNITY_EDITOR
using UnityEngine;

namespace TestUbisoft.Server.AI
{
    [CreateAssetMenu(
        fileName = "LowestPathCostEggSelectionStrategy",
        menuName = "TestUbisoft/Server AI/Lowest Path Cost Egg Selection")]
    public sealed class LowestPathCostEggSelectionStrategy : EggTargetSelectionStrategy
    {
        public override bool TrySelectTarget(BotTargetSelectionContext context, out EggTargetSelection selection)
        {
            selection = default;

            if (context.EggProvider == null || context.Pathfinder == null)
            {
                return false;
            }

            var eggs = context.EggProvider.GetAvailableEggs();
            if (eggs == null || eggs.Count == 0)
            {
                return false;
            }

            var hasBestTarget = false;
            var bestCost = float.PositiveInfinity;
            var bestDistanceSqr = float.PositiveInfinity;
            var bestTarget = default(EggTarget);
            var bestPath = default(PathResult);

            for (var i = 0; i < eggs.Count; i++)
            {
                var egg = eggs[i];
                if (!egg.IsValid || !context.EggProvider.IsEggValid(egg.Id))
                {
                    continue;
                }

                if (!context.Pathfinder.TryFindPath(context.BotPosition, egg.Position, out var path) || !path.IsReachable)
                {
                    continue;
                }

                var distanceSqr = (egg.Position - context.BotPosition).sqrMagnitude;
                if (!IsBetterPath(path.TotalCost, distanceSqr, bestCost, bestDistanceSqr))
                {
                    continue;
                }

                hasBestTarget = true;
                bestCost = path.TotalCost;
                bestDistanceSqr = distanceSqr;
                bestTarget = egg;
                bestPath = path;
            }

            if (!hasBestTarget)
            {
                return false;
            }

            selection = new EggTargetSelection(bestTarget, bestPath);
            return true;
        }

        private static bool IsBetterPath(
            float candidateCost,
            float candidateDistanceSqr,
            float bestCost,
            float bestDistanceSqr)
        {
            if (candidateCost < bestCost)
            {
                return true;
            }

            if (!Mathf.Approximately(candidateCost, bestCost))
            {
                return false;
            }

            return candidateDistanceSqr < bestDistanceSqr;
        }
    }
}
#endif
