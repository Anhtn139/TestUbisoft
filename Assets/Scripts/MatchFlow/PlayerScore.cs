namespace EggCollecting.MatchFlow
{
    public readonly struct PlayerScore
    {
        public PlayerScore(string playerId, string displayName, int eggCount)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            EggCount = eggCount;
        }

        public string PlayerId { get; }
        public string DisplayName { get; }
        public int EggCount { get; }
    }
}
