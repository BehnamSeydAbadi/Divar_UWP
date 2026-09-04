namespace Divar_UWP.Models
{
    public sealed class DivarPostSummary
    {
        public string Token { get; set; }

        public string Title { get; set; }

        public string TopDescription { get; set; }

        public string MiddleDescription { get; set; }

        public string BottomDescription { get; set; }

        public string ThumbnailUrl { get; set; }

        public string BadgeText { get; set; }

        public string LocationText { get; set; }

        public int ImageCount { get; set; }

        public bool HasChat { get; set; }

        public bool HasBadge { get { return !string.IsNullOrWhiteSpace(BadgeText); } }
    }
}
