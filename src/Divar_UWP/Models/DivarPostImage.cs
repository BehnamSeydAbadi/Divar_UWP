using Divar_UWP.Infrastructure;

namespace Divar_UWP.Models
{
    public sealed class DivarPostImage : ObservableObject
    {
        private string _displayUrl;

        public string FullUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string AltText { get; set; }
        public string DisplayUrl { get { return _displayUrl; } set { SetProperty(ref _displayUrl, value); } }
    }
}
