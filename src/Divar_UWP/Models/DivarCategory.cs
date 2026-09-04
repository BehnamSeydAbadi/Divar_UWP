using System.Collections.Generic;

namespace Divar_UWP.Models
{
    public sealed class DivarCategory
    {
        public DivarCategory()
        {
            Children = new List<DivarCategory>();
        }

        public string Name { get; set; }

        public string Slug { get; set; }

        public string IconUrl { get; set; }

        public IList<DivarCategory> Children { get; private set; }
    }
}
