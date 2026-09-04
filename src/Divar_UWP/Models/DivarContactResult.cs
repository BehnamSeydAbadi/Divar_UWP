namespace Divar_UWP.Models
{
    public sealed class DivarContactResult
    {
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
        public bool LoginRequired { get; set; }
        public bool ChallengeRequired { get; set; }
        public bool RateLimited { get; set; }
    }
}
