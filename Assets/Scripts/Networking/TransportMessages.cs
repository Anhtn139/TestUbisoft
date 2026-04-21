using System;
using UnityEngine;

namespace TestUbisoft.Networking
{
    [Serializable]
    public sealed class ClientInputMessage
    {
        public int Sequence;
        public Vector2 MoveInput;
        public bool FirePressed;
        public float ClientTime;
    }

    [Serializable]
    public sealed class ServerSnapshotMessage
    {
        public int Sequence;
        public string PlayerId;
        public Vector3 PlayerPosition;
        public Quaternion PlayerRotation;
        public float ServerTime;
    }
}
