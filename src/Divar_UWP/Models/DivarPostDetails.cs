using System.Collections.Generic;

namespace Divar_UWP.Models
{
    public sealed class DivarPostDetails
    {
        public DivarPostDetails()
        {
            ImageUrls = new List<string>();
            Images = new List<DivarPostImage>();
            Attributes = new List<DivarPostAttribute>();
        }

        public string Token { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string PriceText { get; set; }

        public string ShareUrl { get; set; }

        public string LocationText { get; set; }

        public string BreadcrumbText { get; set; }

        public string SellerInfo { get; set; }

        public string BadgeText { get; set; }

        public bool ChatEnabled { get; set; }

        public IList<string> ImageUrls { get; private set; }

        public IList<DivarPostImage> Images { get; private set; }

        public IList<DivarPostAttribute> Attributes { get; private set; }
    }
}
