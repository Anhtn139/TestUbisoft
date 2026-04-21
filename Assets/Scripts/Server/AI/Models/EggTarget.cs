#if UNITY_SERVER || UNITY_EDITOR
using System;
using UnityEngine;

namespace TestUbisoft.Server.AI
{
    [Serializable]
    public readonly struct EggTarget
    {
        public EggTarget(string id, Vector3 position, bool isValid = true)
        {
            Id = id;
            Position = position;
            IsValid = isValid && !string.IsNullOrWhiteSpace(id);
        }

        public string Id { get; }

        public Vector3 Position { get; }

        public bool IsValid { get; }
    }
}
#endif
