using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;

namespace Divar_UWP.Services
{
    public interface IDivarFilterService
    {
        Task<ServiceResult<IList<DivarFilterDefinition>>> GetFiltersAsync(
            DivarCity city,
            string categorySlug,
            string searchDataJson,
            CancellationToken cancellationToken);
    }
}
