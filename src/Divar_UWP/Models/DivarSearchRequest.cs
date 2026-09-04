using System.Collections.Generic;

namespace Divar_UWP.Models
{
    public sealed class DivarSearchRequest
    {
        public DivarSearchRequest()
        {
            CityIds = new List<string>();
        }

        public IList<string> CityIds { get; private set; }

        public string Query { get; set; }

        public string CategorySlug { get; set; }

        public string PaginationDataJson { get; set; }

        public string SearchDataJson { get; set; }

        public string FilterDataJson { get; set; }
    }
}
