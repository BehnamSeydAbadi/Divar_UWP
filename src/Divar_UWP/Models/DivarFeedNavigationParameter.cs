namespace Divar_UWP.Models
{
    public sealed class DivarFeedNavigationParameter
    {
        public string Query { get; set; }
        public string CategorySlug { get; set; }
        public string Title { get; set; }
        public string FilterDataJson { get; set; }
        public int ActiveFilterCount { get; set; }
    }
}
