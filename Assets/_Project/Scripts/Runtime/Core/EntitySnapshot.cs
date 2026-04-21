namespace TestUbisoft.Prototype.Core
{
    /// <summary>
    /// Presentation-safe entity state produced by the authoritative simulation.
    /// Clients render this data; they do not mutate server-owned gameplay state.
    /// </summary>
    public readonly struct EntitySnapshot
    {
        public readonly int EntityId;
        public readonly string OwnerClientId;
        public readonly EntityKind Kind;
        public readonly SimVector2 Position;
        public readonly float YawDegrees;
        public readonly bool IsActive;
        public readonly int VisualIndex;

        public EntitySnapshot(
            int entityId,
            string ownerClientId,
            EntityKind kind,
            SimVector2 position,
            float yawDegrees,
            bool isActive = true,
            int visualIndex = 0)
        {
            EntityId = entityId;
            OwnerClientId = ownerClientId;
            Kind = kind;
            Position = position;
            YawDegrees = yawDegrees;
            IsActive = isActive;
            VisualIndex = visualIndex;
        }
    }
}
