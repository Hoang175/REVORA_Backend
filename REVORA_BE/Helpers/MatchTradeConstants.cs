namespace REVORA_BE.Helpers
{
    public static class MatchTradeConstants
    {
        public const int DisplayParticipantBoost = 1000;
        public const int DisplayProductBoost = 2500;

        public static readonly (decimal Min, decimal Max, string Label)[] PriceBuckets =
        {
            (0, decimal.MaxValue, "Tất cả mức giá"),
            (0, 100_000, "Dưới 100.000đ"),
            (100_000, 300_000, "100.000đ – 300.000đ"),
            (300_000, 500_000, "300.000đ – 500.000đ"),
            (500_000, 1_000_000, "500.000đ – 1.000.000đ"),
            (1_000_000, decimal.MaxValue, "Trên 1.000.000đ")
        };
    }
}
