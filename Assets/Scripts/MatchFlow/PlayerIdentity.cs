using System;
using UnityEngine;

namespace EggCollecting.MatchFlow
{
    public sealed class PlayerIdentity : MonoBehaviour
    {
        [SerializeField] private string playerId;
        [SerializeField] private string displayName = "Player";

        public string PlayerId
        {
            get
            {
                EnsureId();
                return playerId;
            }
        }

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? PlayerId : displayName;

        public void ConfigureIdentity(string id, string playerDisplayName)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                playerId = id;
            }

            if (!string.IsNullOrWhiteSpace(playerDisplayName))
            {
                displayName = playerDisplayName;
            }

            MatchFlowController activeMatch = MatchFlowController.Active;
            if (isActiveAndEnabled && activeMatch != null)
            {
                activeMatch.ServerRegisterPlayer(this);
            }
        }

        private void Reset()
        {
            EnsureId();
            displayName = gameObject.name;
        }

        private void Awake()
        {
            EnsureId();
        }

        private void OnEnable()
        {
            MatchFlowController activeMatch = MatchFlowController.Active;
            if (activeMatch != null)
            {
                activeMatch.ServerRegisterPlayer(this);
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName) && gameObject != null)
            {
                displayName = gameObject.name;
            }
        }

        private void EnsureId()
        {
            if (!string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            playerId = Guid.NewGuid().ToString("N");
        }
    }
}
