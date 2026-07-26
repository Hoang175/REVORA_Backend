namespace REVORA_BE.Configs
{
    public class JwtSettings
    {
        public string Key { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int AccessTokenExpirationMinutes { get; set; }
        public int RefreshTokenExpirationDays { get; set; }
        public string RefreshTokenCookieName { get; set; } = string.Empty;
        public bool RefreshTokenCookieSecure { get; set; } = true;
        public string RefreshTokenCookieSameSite { get; set; } = "Strict";
    }
}
