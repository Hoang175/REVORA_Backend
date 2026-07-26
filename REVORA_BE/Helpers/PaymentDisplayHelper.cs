namespace REVORA_BE.Helpers
{
    public static class PaymentDisplayHelper
    {
        public static string GetCreditTypeDisplayName(string? creditTypeName) => creditTypeName switch
        {
            "Posting" => "Credit Đăng Tin",
            "Featured" => "Credit Nổi Bật",
            _ => creditTypeName ?? string.Empty
        };
    }
}
