#if UNITY_SERVER || UNITY_EDITOR
using System;
using UnityEngine;

namespace TestUbisoft.Server.AI
{
    [DisallowMultipleComponent]
    public sealed class ServerEggTarget : MonoBehaviour
    {
        [SerializeField] private string eggId;
        [SerializeField] private bool isAvailable = true;

        public string EggId
        {
            get { return eggId; }
        }

        public bool IsAvailable
        {
            get { return isAvailable; }
            set { isAvailable = value; }
        }

        public void Configure(string id, bool available = true)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                eggId = id;
            }

            isAvailable = available;
        }

        public EggTarget ToEggTarget()
        {
            EnsureId();
            return new EggTarget(eggId, transform.position, isAvailable && isActiveAndEnabled);
        }

        public void MarkCollected()
        {
            isAvailable = false;
        }

        private void Awake()
        {
            EnsureId();
        }

        private void OnValidate()
        {
            EnsureId();
        }

        private void EnsureId()
        {
            if (!string.IsNullOrWhiteSpace(eggId))
            {
                return;
            }

            eggId = Guid.NewGuid().ToString("N");
        }
    }
}
#endif
