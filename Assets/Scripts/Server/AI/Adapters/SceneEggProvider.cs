#if UNITY_SERVER || UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace TestUbisoft.Server.AI
{
    [DisallowMultipleComponent]
    public sealed class SceneEggProvider : MonoBehaviour, IEggProvider
    {
        [SerializeField] private bool refreshFromSceneOnEnable = true;
        [SerializeField] private bool includeInactiveEggs;
        [SerializeField] private List<ServerEggTarget> eggs = new List<ServerEggTarget>();

        private readonly List<EggTarget> availableEggs = new List<EggTarget>();

        public IReadOnlyList<EggTarget> GetAvailableEggs()
        {
            availableEggs.Clear();

            for (var i = 0; i < eggs.Count; i++)
            {
                var egg = eggs[i];
                if (egg == null)
                {
                    continue;
                }

                var eggTarget = egg.ToEggTarget();
                if (eggTarget.IsValid)
                {
                    availableEggs.Add(eggTarget);
                }
            }

            return availableEggs;
        }

        public bool TryGetEgg(string eggId, out EggTarget egg)
        {
            for (var i = 0; i < eggs.Count; i++)
            {
                var eggTarget = eggs[i];
                if (eggTarget == null || eggTarget.EggId != eggId)
                {
                    continue;
                }

                egg = eggTarget.ToEggTarget();
                return egg.IsValid;
            }

            egg = default;
            return false;
        }

        public bool IsEggValid(string eggId)
        {
            return TryGetEgg(eggId, out _);
        }

        public void RefreshEggsFromScene()
        {
            eggs.Clear();
            eggs.AddRange(FindObjectsOfType<ServerEggTarget>(includeInactiveEggs));
        }

        private void OnEnable()
        {
            if (refreshFromSceneOnEnable)
            {
                RefreshEggsFromScene();
            }
        }
    }
}
#endif
