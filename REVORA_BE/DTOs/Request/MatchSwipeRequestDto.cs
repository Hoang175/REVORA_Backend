namespace REVORA_BE.DTOs.Request
{
    public class MatchSwipeRequestDto
    {
        public long ProductId { get; set; }

        /// <summary>pass | like</summary>
        public string Direction { get; set; } = null!;
    }
}
