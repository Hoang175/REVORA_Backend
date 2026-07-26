using System;

namespace REVORA_BE.DTOs.Request
{
    public class RenewProductRequestDto
    {
        public bool RenewProduct { get; set; }
        public bool RenewBanner { get; set; }
        public bool RenewShort { get; set; }
        public string? NewBannerUrl { get; set; }
    }
}
