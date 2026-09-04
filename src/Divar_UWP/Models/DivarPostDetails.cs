using System.Collections.Generic;

namespace Divar_UWP.Models
{
    public sealed class DivarPostDetails
    {
        public DivarPostDetails()
        {
            ImageUrls = new List<string>();
        }

        public string Token { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string PriceText { get; set; }

        public string ShareUrl { get; set; }

        public IList<string> ImageUrls { get; private set; }
    }
}
