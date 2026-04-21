using System;

namespace TestUbisoft.Prototype.Core
{
    /// <summary>
    /// Small simulation-space vector used by core/server code.
    /// Keeping this separate from UnityEngine.Vector types makes the simulation easier to move to a real server.
    /// </summary>
    public readonly struct SimVector2 : IEquatable<SimVector2>
    {
        public static readonly SimVector2 Zero = new SimVector2(0f, 0f);

        public readonly float X;
        public readonly float Y;

        public SimVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float SqrMagnitude => (X * X) + (Y * Y);

        public float Magnitude => (float)Math.Sqrt(SqrMagnitude);

        public SimVector2 Normalized
        {
            get
            {
                float magnitude = Magnitude;
                return magnitude > 0.0001f ? this * (1f / magnitude) : Zero;
            }
        }

        public static SimVector2 ClampMagnitude(SimVector2 value, float maxMagnitude)
        {
            float maxSqrMagnitude = maxMagnitude * maxMagnitude;
            return value.SqrMagnitude > maxSqrMagnitude ? value.Normalized * maxMagnitude : value;
        }

        public static SimVector2 operator +(SimVector2 left, SimVector2 right)
        {
            return new SimVector2(left.X + right.X, left.Y + right.Y);
        }

        public static SimVector2 operator -(SimVector2 left, SimVector2 right)
        {
            return new SimVector2(left.X - right.X, left.Y - right.Y);
        }

        public static SimVector2 operator *(SimVector2 value, float scalar)
        {
            return new SimVector2(value.X * scalar, value.Y * scalar);
        }

        public bool Equals(SimVector2 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is SimVector2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"({X:0.###}, {Y:0.###})";
        }
    }
}
