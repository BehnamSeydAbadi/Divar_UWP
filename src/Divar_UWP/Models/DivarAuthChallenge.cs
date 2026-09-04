namespace Divar_UWP.Models
{
    public sealed class DivarAuthChallenge
    {
        public string PhoneNumber { get; set; }

        public string DeviceId { get; set; }

        public string PreAuthSessionId { get; set; }
    }
}
