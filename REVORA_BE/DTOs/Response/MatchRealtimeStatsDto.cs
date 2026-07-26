namespace REVORA_BE.DTOs.Response
{
    public class MatchRealtimeStatsDto
    {
        public MatchCommunityStatsDto CommunityStats { get; set; } = null!;
        public MatchFilterOptionsDto FilterOptions { get; set; } = null!;
    }
}
