using TestUbisoft.Prototype.Core;
using UnityEngine;

namespace TestUbisoft.Prototype.Presentation
{
    public static class SimUnityConversions
    {
        public static Vector3 ToUnityPosition(this SimVector2 position, float y = 0f)
        {
            return new Vector3(position.X, y, position.Y);
        }

        public static SimVector2 ToSimVector2(this Vector2 input)
        {
            return new SimVector2(input.x, input.y);
        }

        public static Quaternion ToUnityYaw(this float yawDegrees)
        {
            return Quaternion.Euler(0f, yawDegrees, 0f);
        }
    }
}
