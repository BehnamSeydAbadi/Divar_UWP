using System.Collections.Generic;

namespace Divar_UWP.Models
{
    public sealed class DivarPage<T>
    {
        public DivarPage()
        {
            Items = new List<T>();
        }

        public IList<T> Items { get; private set; }

        public bool HasNextPage { get; set; }

        public string PaginationDataJson { get; set; }
    }
}
