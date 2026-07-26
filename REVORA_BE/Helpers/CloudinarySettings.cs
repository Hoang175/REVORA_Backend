namespace REVORA_BE.Helpers
{
    // Class này dùng để "hứng" dữ liệu từ appsettings.json
    public class CloudinarySettings
    {
        public string CloudName { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public string ApiSecret { get; set; } = null!;
    }
}