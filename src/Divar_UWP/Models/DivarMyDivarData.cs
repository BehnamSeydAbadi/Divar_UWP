using System.Collections.Generic;

namespace Divar_UWP.Models
{
    public sealed class DivarMyDivarData
    {
        public DivarMyDivarData()
        {
            Posts = new List<DivarPostSummary>();
        }

        public string DisplayName { get; set; }

        public string PhoneNumber { get; set; }

        public IList<DivarPostSummary> Posts { get; private set; }
    }
}
