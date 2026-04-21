namespace TestUbisoft.Prototype.Core
{
    public readonly struct ScoreSnapshot
    {
        public readonly string PlayerId;
        public readonly string DisplayName;
        public readonly int EggCount;

        public ScoreSnapshot(string playerId, string displayName, int eggCount)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            EggCount = eggCount;
        }
    }
}
