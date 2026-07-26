namespace REVORA_BE.Helpers
{
    public class PayOSSettings
    {
        public string ClientId { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public string ChecksumKey { get; set; } = null!;
        public string ReturnUrl { get; set; } = null!;
        public string CancelUrl { get; set; } = null!;
    }
}
