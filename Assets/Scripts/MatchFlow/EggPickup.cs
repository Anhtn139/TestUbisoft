using UnityEngine;

namespace EggCollecting.MatchFlow
{
    public sealed class EggPickup : MonoBehaviour
    {
        [SerializeField] private string eggId;
        [SerializeField] private bool disableWhenCollected = true;

        private bool isCollected;

        public string EggId => string.IsNullOrWhiteSpace(eggId) ? name : eggId;
        public bool IsCollected => isCollected;

        public void ConfigureId(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                eggId = id;
            }
        }

        private void Reset()
        {
            eggId = gameObject.name;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(eggId) && gameObject != null)
            {
                eggId = gameObject.name;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryRequestCollection(other.GetComponentInParent<PlayerIdentity>());
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryRequestCollection(other.GetComponentInParent<PlayerIdentity>());
        }

        public void ServerResetForMatch()
        {
            isCollected = false;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public void ServerMarkCollected()
        {
            isCollected = true;

            if (disableWhenCollected)
            {
                gameObject.SetActive(false);
            }
        }

        private void TryRequestCollection(PlayerIdentity player)
        {
            if (player == null || isCollected)
            {
                return;
            }

            MatchFlowController activeMatch = MatchFlowController.Active;
            if (activeMatch != null)
            {
                activeMatch.ServerResolveEggCollection(player, this);
            }
        }
    }
}
